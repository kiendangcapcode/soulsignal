(() => {
    const player = document.querySelector("[data-site-player]");
    const tracks = Array.from(document.querySelectorAll("[data-track-play]"));
    if (!player || !tracks.length) return;

    const control = name => player.querySelector(`[data-player-${name}]`);
    const audio = control("audio");
    const cover = control("cover");
    const title = control("title");
    const artist = control("artist");
    const toggle = control("toggle");
    const previous = control("prev");
    const next = control("next");
    const elapsed = control("elapsed");
    const duration = control("duration");
    const seek = control("seek");
    const volume = control("volume");
    const mute = control("mute");
    const download = control("download");
    const close = control("close");
    const page = document.documentElement;
    const otherAudio = document.querySelectorAll("audio:not([data-player-audio])");
    const native = document.querySelector("[data-player-native]");
    const qualityFallback = document.querySelector("[data-quality-fallback]");
    const qualityPickers = Array.from(document.querySelectorAll("[data-quality-picker]"), picker => ({
        picker,
        forDetail: picker.dataset.qualityPicker === "detail",
        summary: picker.querySelector("summary"),
        label: picker.querySelector("[data-quality-label]"),
        mp3: picker.querySelector('[data-quality-option="mp3"]'),
        mp3Label: picker.querySelector("[data-quality-mp3-label]"),
        flac: picker.querySelector('[data-quality-option="flac"]'),
        vip: picker.querySelector("[data-quality-vip]"),
        unavailable: picker.querySelector("[data-quality-unavailable]")
    }));
    const supportsFlac = Boolean(audio.canPlayType("audio/flac"));
    const preferredQualities = new Map();
    const tracksById = new Map(tracks.map(track => [track.dataset.trackId, track]));
    const favoriteControls = Array.from(document.querySelectorAll("[data-favorite-form]"), form => ({
        form,
        id: form.elements.namedItem("id"),
        isFavorite: form.elements.namedItem("isFavorite"),
        button: form.querySelector("[data-favorite-button]")
    }));
    const playerFavorite = favoriteControls.find(({ form }) => form.closest("[data-player-favorite]"));
    const favoriteStatus = document.querySelector("[data-favorite-status]");
    const favoriteCount = document.querySelector("[data-favorite-count]");
    const favoriteEmpty = document.querySelector("[data-favorite-empty]");
    const pendingFavorites = new Set();

    let current = -1;
    let flac = false;
    let qualityStatus = "";
    let wantsPlayback = false;
    let sourceVersion = 0;
    let resumeAt = 0;
    let favoriteStatusTimeout;

    const formatTime = seconds => {
        const total = Math.max(0, Math.floor(Number.isFinite(seconds) ? seconds : 0));
        return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, "0")}`;
    };

    function renderIcon(button, icon) {
        button.querySelector("use").setAttribute("href", `#i-player-${icon}`);
    }

    function renderPlayback() {
        const playing = current >= 0 && !audio.paused && !audio.ended;
        const label = playing ? "Tạm dừng" : "Phát";
        toggle.setAttribute("aria-label", label);
        toggle.title = label;
        renderIcon(toggle, playing ? "pause" : "play");
        tracks.forEach((track, index) => {
            const active = index === current;
            const action = active ? label : "Phát";
            track.classList.toggle("is-current", active);
            track.setAttribute("aria-label", `${action} ${track.dataset.trackTitle}`);
            track.querySelector("[data-play-label]").textContent = action;
            renderIcon(track, active && playing ? "pause" : "play");
        });
    }

    function renderTimeline() {
        const total = Number.isFinite(audio.duration) && audio.duration > 0 ? audio.duration : 0;
        const position = total ? Math.min(audio.currentTime, total) : 0;
        elapsed.textContent = formatTime(position);
        duration.textContent = formatTime(total || Number(tracks[current]?.dataset.trackDuration));
        seek.disabled = !total;
        seek.max = total;
        seek.value = position;
        seek.style.setProperty("--range-fill", `${total ? position / total * 100 : 0}%`);
        seek.setAttribute("aria-valuetext", `${formatTime(position)} trên ${duration.textContent}`);
    }

    function renderVolume() {
        const silent = audio.muted || audio.volume === 0;
        const label = silent ? "Bật tiếng" : "Tắt tiếng";
        mute.setAttribute("aria-label", label);
        mute.title = label;
        renderIcon(mute, silent ? "muted" : "volume");
        volume.value = audio.muted ? 0 : audio.volume;
        volume.style.setProperty("--range-fill", `${Number(volume.value) * 100}%`);
        volume.setAttribute("aria-valuetext", `${Math.round(Number(volume.value) * 100)}%`);
    }

    function preferredFlac(index) {
        return preferredQualities.get(index) ?? (supportsFlac && Boolean(tracks[index].dataset.trackFlac));
    }

    function renderQuality() {
        for (const menu of qualityPickers) {
            const index = menu.forDetail ? 0 : current;
            menu.picker.hidden = index < 0;
            if (index < 0) {
                menu.picker.open = false;
                continue;
            }

            const track = tracks[index].dataset;
            const flacAvailable = supportsFlac && Boolean(track.trackFlac);
            const flacLocked = track.trackHasFlac === "true" && !track.trackFlac;
            const selectedFlac = index === current ? flac : preferredFlac(index);
            const format = selectedFlac ? "FLAC" : track.trackMp3Label;
            const label = (index === current && qualityStatus) || format;
            if (menu.label.textContent !== label) menu.label.textContent = label;
            menu.mp3Label.textContent = track.trackMp3Label;
            menu.mp3.setAttribute("aria-pressed", String(!selectedFlac));
            menu.flac.setAttribute("aria-pressed", String(selectedFlac));
            menu.flac.hidden = !flacAvailable;
            menu.vip.hidden = !flacLocked;
            menu.unavailable.hidden = flacAvailable || flacLocked;
        }
    }

    function chooseQuality(menu, useFlac) {
        const index = menu.forDetail ? 0 : current;
        if (index < 0 || (useFlac && (!supportsFlac || !tracks[index].dataset.trackFlac))) return;

        menu.picker.open = false;
        menu.summary.focus();
        preferredQualities.set(index, useFlac);
        if (index === current && (flac !== useFlac || audio.error)) {
            loadSource(useFlac, audio.currentTime || resumeAt, wantsPlayback);
        } else {
            renderQuality();
        }
    }

    function reserveSpace() {
        page.style.setProperty("--player-space", `${player.offsetHeight}px`);
    }

    function adjacentTrack(direction) {
        for (let index = current + direction; tracks[index]; index += direction) {
            if (!tracks[index].closest("[data-favorite-row][hidden]")) return index;
        }
        return -1;
    }

    function renderQueue() {
        previous.disabled = adjacentTrack(-1) < 0;
        next.disabled = adjacentTrack(1) < 0;
    }

    function renderFavorites() {
        if (playerFavorite && current >= 0) playerFavorite.id.value = tracks[current].dataset.trackId;
        for (const control of favoriteControls) {
            const track = tracksById.get(control.id.value);
            if (!track) continue;
            const saved = track.dataset.trackFavorite === "true";
            control.isFavorite.value = String(!saved);
            control.button.setAttribute("aria-pressed", String(saved));
            control.button.setAttribute("aria-disabled", String(pendingFavorites.has(control.id.value)));
            control.button.setAttribute("aria-label", `Yêu thích ${track.dataset.trackTitle}`);
            control.button.title = saved ? "Bỏ yêu thích" : "Thêm vào Yêu thích";
        }
    }

    function updateFavoritesList(track, isFavorite) {
        const row = track.closest("[data-favorite-row]");
        if (!row) return;
        if (!isFavorite && row.contains(document.activeElement)) {
            document.querySelector("#favorites-title").focus();
        }
        row.hidden = !isFavorite;
        const count = document.querySelectorAll("[data-favorite-row]:not([hidden])").length;
        favoriteCount.textContent = `${count} bài hát`;
        favoriteEmpty.hidden = count > 0;
        if (current >= 0) renderQueue();
    }

    function announceFavorite(message) {
        window.clearTimeout(favoriteStatusTimeout);
        favoriteStatus.textContent = message;
        favoriteStatusTimeout = window.setTimeout(() => {
            favoriteStatus.textContent = "";
        }, 3500);
    }

    async function saveFavorite(event, control) {
        event.preventDefault();
        const id = control.id.value;
        const track = tracksById.get(id);
        if (!track || pendingFavorites.has(id)) return;

        const body = new FormData(control.form);
        pendingFavorites.add(id);
        renderFavorites();
        try {
            const response = await fetch(control.form.action, {
                method: "POST",
                headers: { Accept: "application/json" },
                body
            });
            if (!response.ok || response.redirected) throw new Error("Favorite request failed.");
            const { isFavorite } = await response.json();
            if (typeof isFavorite !== "boolean") throw new Error("Invalid favorite response.");
            track.dataset.trackFavorite = String(isFavorite);
            updateFavoritesList(track, isFavorite);
            announceFavorite(isFavorite ? "Đã lưu vào Bài hát yêu thích." : "Đã bỏ khỏi Bài hát yêu thích.");
        } catch {
            announceFavorite("Chưa cập nhật được bài hát yêu thích. Vui lòng thử lại.");
        } finally {
            pendingFavorites.delete(id);
            renderFavorites();
        }
    }

    function play() {
        wantsPlayback = true;
        const version = sourceVersion;
        audio.play().catch(error => {
            if (version !== sourceVersion || error.name === "AbortError" || audio.error) return;
            wantsPlayback = false;
            qualityStatus = "Nhấn Phát để thử lại";
            renderQuality();
            renderPlayback();
        });
        renderPlayback();
    }

    function loadSource(useFlac, position, shouldPlay) {
        const track = tracks[current].dataset;
        sourceVersion++;
        flac = useFlac;
        resumeAt = position;
        qualityStatus = "Đang tải";
        audio.preload = shouldPlay ? "none" : "metadata";
        audio.src = useFlac ? track.trackFlac : track.trackMp3;
        renderQuality();
        renderTimeline();
        if (shouldPlay) play();
        else renderPlayback();
    }

    function select(index) {
        if (!tracks[index]) return;
        current = index;
        const track = tracks[current].dataset;
        cover.src = track.trackCover;
        title.href = track.trackDetail;
        title.textContent = track.trackTitle;
        artist.textContent = track.trackArtist;
        renderQueue();
        renderFavorites();
        download.hidden = !track.trackDownload;
        if (track.trackDownload) download.href = track.trackDownload;
        else download.removeAttribute("href");
        player.hidden = false;
        reserveSpace();
        loadSource(preferredFlac(index), 0, true);
    }

    function togglePlayback() {
        if (current < 0) return;
        if (!audio.paused) {
            wantsPlayback = false;
            audio.pause();
        } else if (audio.error) {
            select(current);
        } else {
            play();
        }
    }

    function dismiss(restoreFocus) {
        if (current < 0) return;
        const trigger = tracks[current];
        current = -1;
        qualityStatus = "";
        wantsPlayback = false;
        sourceVersion++;
        audio.pause();
        audio.removeAttribute("src");
        audio.load();
        player.hidden = true;
        page.style.removeProperty("--player-space");
        renderPlayback();
        renderQuality();
        if (restoreFocus) {
            const target = trigger.closest("[data-favorite-row][hidden]")
                ? document.querySelector("#favorites-title") : trigger;
            target.focus({ preventScroll: true });
        }
    }

    tracks.forEach((track, index) => {
        track.hidden = false;
        track.addEventListener("click", event => {
            if (event.button !== 0 || event.metaKey || event.ctrlKey || event.altKey || event.shiftKey) return;
            event.preventDefault();
            if (index === current) togglePlayback();
            else select(index);
        });
    });
    if (native) native.hidden = true;
    if (qualityFallback) qualityFallback.hidden = true;
    for (const favorite of favoriteControls) {
        favorite.form.addEventListener("submit", event => saveFavorite(event, favorite));
    }
    for (const menu of qualityPickers) {
        menu.mp3.addEventListener("click", () => chooseQuality(menu, false));
        menu.flac.addEventListener("click", () => chooseQuality(menu, true));
        menu.picker.addEventListener("keydown", event => {
            if (event.key !== "Escape" || !menu.picker.open) return;
            event.preventDefault();
            menu.picker.open = false;
            menu.summary.focus();
        });
    }
    document.addEventListener("click", event => {
        for (const { picker } of qualityPickers) {
            if (picker.open && !picker.contains(event.target)) picker.open = false;
        }
    });
    toggle.addEventListener("click", togglePlayback);
    previous.addEventListener("click", () => select(adjacentTrack(-1)));
    next.addEventListener("click", () => select(adjacentTrack(1)));
    close.addEventListener("click", () => dismiss(true));
    window.addEventListener("pagehide", () => dismiss(false));
    if (window.ResizeObserver) new ResizeObserver(reserveSpace).observe(player);
    else window.addEventListener("resize", reserveSpace);

    seek.addEventListener("input", () => {
        if (seek.disabled) return;
        audio.currentTime = Number(seek.value);
        renderTimeline();
    });
    volume.addEventListener("input", () => {
        audio.volume = Number(volume.value);
        audio.muted = false;
    });
    mute.addEventListener("click", () => {
        if (audio.volume === 0) audio.volume = 1;
        else audio.muted = !audio.muted;
    });

    audio.addEventListener("loadedmetadata", () => {
        if (current < 0) return;
        if (resumeAt > 0 && Number.isFinite(audio.duration)) {
            audio.currentTime = Math.min(resumeAt, audio.duration);
            resumeAt = 0;
        }
        qualityStatus = "";
        renderQuality();
        renderTimeline();
    });
    audio.addEventListener("timeupdate", renderTimeline);
    audio.addEventListener("durationchange", renderTimeline);
    audio.addEventListener("volumechange", renderVolume);
    audio.addEventListener("play", () => {
        otherAudio.forEach(preview => preview.pause());
        renderPlayback();
    });
    otherAudio.forEach(preview => preview.addEventListener("play", () => {
        wantsPlayback = false;
        audio.pause();
    }));
    audio.addEventListener("pause", renderPlayback);
    audio.addEventListener("ended", () => {
        const following = current >= 0 ? adjacentTrack(1) : -1;
        if (following >= 0) select(following);
        else {
            wantsPlayback = false;
            renderPlayback();
        }
    });
    audio.addEventListener("error", () => {
        if (current < 0 || !audio.error) return;
        if (flac) loadSource(false, audio.currentTime || resumeAt, wantsPlayback);
        else {
            wantsPlayback = false;
            qualityStatus = "Không phát được bài này";
            renderQuality();
            renderPlayback();
        }
    });
    renderQuality();
    renderVolume();
})();
