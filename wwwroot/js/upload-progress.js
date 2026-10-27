(() => {
    "use strict";

    // The multipart byte count and the encoder percentage are separate measurements.
    if (!window.XMLHttpRequest || !window.FormData || !window.fetch || !window.AbortController) return;

    document.querySelectorAll("form[data-upload-form]").forEach((form) => {
        const submit = form.querySelector('button[type="submit"]');
        const panel = form.querySelector("[data-upload-progress]");
        const phase = form.querySelector("[data-upload-phase]");
        const number = form.querySelector("[data-upload-number]");
        const meter = form.querySelector("[data-upload-meter]");
        const message = form.querySelector("[data-upload-message]");
        const errors = form.querySelector("[data-upload-errors]");
        const cancel = form.querySelector("[data-upload-cancel]");
        const originalButton = submit.innerHTML;
        let current = null;

        function showProgress(title, detail, percent) {
            panel.hidden = false;
            phase.textContent = title;
            message.textContent = detail;
            if (typeof percent === "number" && Number.isFinite(percent)) {
                const value = Math.max(0, Math.min(100, Math.round(percent)));
                meter.value = value;
                number.textContent = `${value}%`;
            } else {
                meter.removeAttribute("value");
                number.textContent = "";
            }
        }

        function stop(run) {
            run.finished = true;
            clearTimeout(run.pollTimer);
            clearTimeout(run.deadlineTimer);
            run.startController.abort();
            if (run.pollController) run.pollController.abort();
            if (current === run) current = null;
            form.removeAttribute("aria-busy");
            submit.disabled = false;
            submit.innerHTML = originalButton;
            cancel.hidden = true;
        }

        function fail(run, detail, fields) {
            if (run.finished) return;
            stop(run);
            if (run.xhr && run.xhr.readyState !== XMLHttpRequest.DONE) run.xhr.abort();
            if (run.uploadId && !run.serverCompleted) {
                const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
                fetch(`/UploadProgress/Cancel/${encodeURIComponent(run.uploadId)}`, {
                    method: "POST", credentials: "same-origin", keepalive: true,
                    body: new URLSearchParams({ __RequestVerificationToken: token }),
                }).catch(() => {});
            }
            panel.hidden = true;
            errors.replaceChildren();
            const list = document.createElement("ul");
            const items = Array.isArray(fields) && fields.length ? fields : [{ message: detail }];
            items.forEach((item) => {
                const text = typeof item.message === "string" ? item.message : detail;
                const line = document.createElement("li");
                line.textContent = text;
                list.appendChild(line);
                if (item.field) {
                    form.querySelectorAll("[data-valmsg-for]").forEach((span) => {
                        if (span.dataset.valmsgFor === item.field) {
                            span.textContent = text;
                            span.classList.add("field-validation-error");
                        }
                    });
                }
            });
            errors.appendChild(list);
            errors.hidden = false;
            errors.focus({ preventScroll: true });
            errors.scrollIntoView({ block: "center", behavior: "smooth" });
        }

        function clearErrors() {
            errors.hidden = true;
            errors.replaceChildren();
            form.querySelectorAll("[data-valmsg-for]").forEach((span) => {
                span.textContent = "";
                span.classList.remove("field-validation-error");
            });
            form.querySelectorAll(".input-validation-error").forEach((input) => input.classList.remove("input-validation-error"));
            const summary = form.querySelector(".validation-summary-errors");
            if (summary) {
                summary.replaceChildren();
                summary.classList.remove("validation-summary-errors");
            }
        }

        function processing(run, status) {
            const detail = typeof status.message === "string" && status.message
                ? status.message : "Tệp đã tải lên xong. Máy chủ đang xử lý bản nhạc.";
            switch (status.phase) {
                case "failed":
                case "cancelled":
                    fail(run, detail);
                    return;
                case "encoding":
                    showProgress("Đang chuyển mã MP3", detail, typeof status.percent === "number" ? Math.min(99, status.percent) : null);
                    break;
                case "cover":
                    showProgress("Đang chuẩn bị ảnh bìa", detail);
                    break;
                case "saving":
                    showProgress("Đang lưu bài chờ duyệt", detail);
                    break;
                case "completed":
                    run.serverCompleted = true;
                    showProgress("Đã xử lý xong", "Đang mở hồ sơ bài hát.", 100);
                    cancel.hidden = true;
                    break;
                default:
                    showProgress("Đang kiểm tra bản nhạc", detail);
            }
        }

        async function poll(run) {
            if (run.finished || current !== run) return;
            run.pollController = new AbortController();
            const timeout = setTimeout(() => run.pollController.abort(), 10000);
            try {
                const response = await fetch(`${form.dataset.progressStatus}/${encodeURIComponent(run.uploadId)}`, {
                    credentials: "same-origin", cache: "no-store", headers: { Accept: "application/json" },
                    signal: run.pollController.signal,
                });
                if (run.finished) return;
                if (response.status === 401 || response.status === 403 || response.redirected) {
                    fail(run, "Phiên đăng nhập đã hết hoặc không còn quyền tải nhạc. Hãy đăng nhập lại và kiểm tra danh mục trước khi gửi tiếp.");
                    return;
                }
                if (response.status === 404) {
                    fail(run, "Không còn tìm thấy phiên xử lý này. Hãy kiểm tra danh mục bài đã gửi trước khi thử lại.");
                    return;
                }
                if (!response.ok) throw new Error("Progress temporarily unavailable");
                const status = await response.json();
                if (!run.finished) processing(run, status);
            } catch (_) {
                if (!run.finished) message.textContent = "Tệp đã tải lên xong. Đang chờ cập nhật tiến độ từ máy chủ…";
            } finally {
                clearTimeout(timeout);
                if (!run.finished && !run.serverCompleted) run.pollTimer = setTimeout(() => poll(run), 750);
            }
        }

        cancel.addEventListener("click", () => {
            if (current) fail(current, "Đã yêu cầu dừng tải và xử lý. Hãy kiểm tra danh mục bài đã gửi trước khi thử lại.");
        });

        form.addEventListener("submit", async (event) => {
            event.preventDefault();
            if (current || !form.reportValidity()) return;
            clearErrors();
            const run = { finished: false, startController: new AbortController() };
            current = run;
            submit.disabled = true;
            submit.textContent = "Đang gửi nhạc…";
            form.setAttribute("aria-busy", "true");
            cancel.hidden = false;
            showProgress("Đang chuẩn bị tải lên", "Đang mở phiên tải nhạc.");
            panel.scrollIntoView({ block: "center", behavior: "smooth" });
            run.deadlineTimer = setTimeout(() => fail(run,
                "Chưa nhận được kết quả sau 4 phút. Hãy kiểm tra kết nối và danh mục bài đã gửi trước khi thử lại."), 240000);
            try {
                const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
                const start = await fetch(form.dataset.progressStart, {
                    method: "POST", credentials: "same-origin", signal: run.startController.signal,
                    headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" },
                    body: new URLSearchParams({ __RequestVerificationToken: token }),
                });
                if (run.finished) return;
                if (!start.ok || start.redirected || !(start.headers.get("content-type") || "").includes("application/json")) {
                    throw new Error("Chưa mở được phiên tải nhạc. Hãy tải lại trang để làm mới đăng nhập rồi thử lại.");
                }
                const result = await start.json();
                if (!result.uploadId || !/^[a-f\d]{32}$/i.test(result.uploadId)) throw new Error("Chưa mở được phiên tải nhạc. Vui lòng thử lại.");
                run.uploadId = result.uploadId;
                if (run.finished) return;
                const payload = new FormData(form);
                payload.set("UploadId", run.uploadId);
                const xhr = run.xhr = new XMLHttpRequest();
                xhr.open("POST", form.action);
                xhr.setRequestHeader("Accept", "application/json");
                xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                xhr.timeout = 240000;
                xhr.upload.addEventListener("progress", (progress) => {
                    if (run.finished) return;
                    const percent = progress.lengthComputable ? progress.loaded / progress.total * 100 : null;
                    showProgress("Đang tải tệp lên", "Đang gửi tệp nhạc và thông tin lên máy chủ.", percent);
                });
                xhr.upload.addEventListener("load", () => {
                    if (run.finished) return;
                    showProgress("Đã tải tệp lên · 100%", "Máy chủ đang kiểm tra FLAC và tạo các bản MP3.");
                    poll(run);
                });
                xhr.addEventListener("load", () => {
                    if (run.finished) return;
                    let response;
                    try { response = JSON.parse(xhr.responseText); } catch (_) { /* Session redirects and server errors can return HTML. */ }
                    if (xhr.status >= 200 && xhr.status < 300 && response && typeof response.redirectUrl === "string") {
                        const target = new URL(response.redirectUrl, window.location.href);
                        if (target.origin !== window.location.origin) {
                            fail(run, "Không mở được hồ sơ bài hát. Hãy kiểm tra danh mục bài đã gửi.");
                            return;
                        }
                        stop(run);
                        showProgress("Đã gửi bài thành công", "Đang mở hồ sơ bài hát.", 100);
                        window.location.assign(target.href);
                        return;
                    }
                    const login = xhr.status === 401 || xhr.status === 403 || /\/Account\/Login(?:\?|$)/i.test(xhr.responseURL);
                    fail(run, login ? "Phiên đăng nhập đã hết hoặc không còn quyền gửi bài. Hãy đăng nhập lại."
                        : response && typeof response.message === "string" ? response.message
                        : "Chưa gửi được bản nhạc. Kiểm tra thông tin bên dưới hoặc thử lại.", response && response.errors);
                });
                xhr.addEventListener("error", () => fail(run, "Mất kết nối khi tải hoặc xử lý nhạc. Hãy kiểm tra mạng và danh mục bài đã gửi trước khi thử lại."));
                xhr.addEventListener("timeout", () => fail(run, "Máy chủ chưa phản hồi kịp. Hãy kiểm tra danh mục bài đã gửi trước khi thử lại."));
                xhr.addEventListener("abort", () => {
                    if (!run.finished) fail(run, "Đã dừng tải nhạc. Bạn có thể kiểm tra lại rồi gửi tiếp.");
                });
                showProgress("Đang tải tệp lên", "Đang gửi tệp nhạc và thông tin lên máy chủ.", 0);
                xhr.send(payload);
            } catch (error) {
                if (!run.finished) fail(run, error.message || "Chưa kết nối được tới máy chủ. Vui lòng thử lại.");
            }
        });
    });
})();
