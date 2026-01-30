(function () {
  const joinState = document.getElementById('joinState');
  const inClassState = document.getElementById('inClassState');
  const classNameEl = document.getElementById('className');
  const classDescEl = document.getElementById('classDesc');

  const timeRangeEl = document.getElementById('studentLectureTimeRange');
  const remainingEl = document.getElementById('studentLectureRemaining');
  const trackEl = document.getElementById('studentLectureProgressTrack');
  const fillEl = document.getElementById('studentLectureProgressFill');
  const nowLineEl = document.getElementById('studentLectureNowLine');

  const joinBtn = document.getElementById('joinClassBtn');
  const classIdInput = document.getElementById('classIdInput');

  if (!joinState || !inClassState || !classNameEl || !classDescEl) return;

  let lectureProgressTimer = null;
  let activeLectureWindow = null; // { start: Date, end: Date }

  // Map to track lecture statuses by id for calendar pills (shared with calendar renderer)
  window._lectureStatusMap = window._lectureStatusMap || {};

  function showJoin() {
    joinState.hidden = false;
    inClassState.hidden = true;

    activeLectureWindow = null;
    if (lectureProgressTimer) {
      clearInterval(lectureProgressTimer);
      lectureProgressTimer = null;
    }
  }

  function getLectureTimeWindow(item) {
    const startRaw = item?.startTime ?? item?.StartTime ?? null;
    const endRaw = item?.endTime ?? item?.EndTime ?? null;
    const duration = item?.duration ?? item?.Duration ?? null;

    if (!startRaw) return null;
    const start = new Date(startRaw);
    if (Number.isNaN(start.getTime())) return null;

    if (endRaw) {
      const end = new Date(endRaw);
      if (!Number.isNaN(end.getTime())) return { start, end };
    }

    const minutes = parseDurationToMinutes(duration);
    const end = new Date(start.getTime() + (minutes * 60 * 1000));
    return { start, end };
  }

  function formatTimeRange(start, end) {
    try {
      const s = start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const e = end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      return `${s} – ${e}`;
    } catch (e) {
      return '';
    }
  }

  function humanizeMinutes(mins) {
    const m = Math.max(0, Math.round(mins));
    if (m < 60) return `${m}m`;
    const h = Math.floor(m / 60);
    const rem = m % 60;
    return rem ? `${h}h ${rem}m` : `${h}h`;
  }

  function renderLectureProgress() {
    if (!activeLectureWindow || !trackEl || !fillEl || !nowLineEl || !timeRangeEl || !remainingEl) return;

    const start = activeLectureWindow.start;
    const end = activeLectureWindow.end;
    const now = new Date();

    const totalMs = Math.max(1, end.getTime() - start.getTime());
    const rawMs = now.getTime() - start.getTime();
    const clampedMs = Math.max(0, Math.min(totalMs, rawMs));
    const pct = clampedMs / totalMs;

    // Update labels
    timeRangeEl.textContent = formatTimeRange(start, end);

    if (now < start) {
      const minsToStart = (start.getTime() - now.getTime()) / (60 * 1000);
      remainingEl.textContent = `Starts in ${humanizeMinutes(minsToStart)}`;
      try { trackEl.setAttribute('aria-valuetext', `Starts in ${humanizeMinutes(minsToStart)}`); } catch (e) { /* ignore */ }
    } else if (now >= end) {
      remainingEl.textContent = 'Ended';
      try { trackEl.setAttribute('aria-valuetext', 'Ended'); } catch (e) { /* ignore */ }
    } else {
      const minsLeft = (end.getTime() - now.getTime()) / (60 * 1000);
      remainingEl.textContent = `${humanizeMinutes(minsLeft)} left`;
      try { trackEl.setAttribute('aria-valuetext', `${humanizeMinutes(minsLeft)} left`); } catch (e) { /* ignore */ }
    }

    // Position now-line + fill
    const pct100 = Math.max(0, Math.min(100, pct * 100));
    nowLineEl.style.left = `${pct100}%`;
    fillEl.style.width = `${pct100}%`;

    try { trackEl.setAttribute('aria-valuenow', String(Math.round(pct100))); } catch (e) { /* ignore */ }
  }

  function showInClass(item) {
    const name = item?.name ?? item?.Name ?? '';
    const desc = item?.description ?? item?.Description ?? '';
    classNameEl.textContent = name;
    classDescEl.textContent = desc;

    activeLectureWindow = getLectureTimeWindow(item);
    if (trackEl && fillEl && nowLineEl && timeRangeEl && remainingEl) {
      if (activeLectureWindow) {
        trackEl.hidden = false;
        renderLectureProgress();
        if (!lectureProgressTimer) {
          lectureProgressTimer = setInterval(renderLectureProgress, 15000);
        }
      } else {
        // If we can't compute timing, keep the UI tidy.
        timeRangeEl.textContent = '';
        remainingEl.textContent = '';
        trackEl.hidden = true;
        if (lectureProgressTimer) {
          clearInterval(lectureProgressTimer);
          lectureProgressTimer = null;
        }
      }
    }

    joinState.hidden = true;
    inClassState.hidden = false;
  }

  function normalizeStatus(status) {
    if (status == null) return null;
    if (typeof status === 'number') return status;
    return String(status).trim().toLowerCase();
  }

  // Active == InProgress (string) or 1 (enum value)
  function isActiveLectureStatus(status) {
    const s = normalizeStatus(status);
    return s === 1 || s === 'inprogress' || s === 'in progress';
  }

  function parseDurationToMinutes(input) {
    if (input == null) return 60;
    if (typeof input === 'number') return Math.max(0, Math.floor(input));
    const s = String(input).trim();
    if (/^P/i.test(s)) {
      const up = s.toUpperCase();
      const h = Number((up.match(/(\d+)H/) || [0, 0])[1] || 0);
      const m = Number((up.match(/(\d+)M/) || [0, 0])[1] || 0);
      const sec = Number((up.match(/(\d+)S/) || [0, 0])[1] || 0);
      return h * 60 + m + Math.round(sec / 60);
    }
    const hhmmss = s.match(/^(\d{1,2}):(\d{2}):(\d{2})$/);
    if (hhmmss) return Number(hhmmss[1]) * 60 + Number(hhmmss[2]) + Math.round(Number(hhmmss[3]) / 60);
    const hhmm = s.match(/^(\d{1,2}):(\d{2})$/);
    if (hhmm) return Number(hhmm[1]) * 60 + Number(hhmm[2]);
    const hm = s.match(/^(\d+)h(?:([0-9]+)m)?$/i);
    if (hm) return Number(hm[1]) * 60 + Number(hm[2] || 0);
    const mins = s.match(/^(\d+)(?:m)?$/i);
    if (mins) return Number(mins[1]);
    return 60;
  }

  async function fetchStudentLectures({ page = 0, pageSize = 200, fromMonthsAgo = null, status = null } = {}) {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    if (fromMonthsAgo != null) params.set('fromMonthsAgo', String(fromMonthsAgo));
    if (status != null) params.set('status', String(status));

    const candidates = [
      '/api/lectures/student?' + params.toString(),
      '/api/lectrues/student?' + params.toString(),
      '/api/lectures?' + params.toString()
    ];

    let lastNon404 = null;

    for (const url of candidates) {
      try {
        const resp = await fetch(url, {
          method: 'GET',
          credentials: 'same-origin',
          headers: { 'Accept': 'application/json' }
        });

        if (resp.status === 404) continue;
        lastNon404 = resp;

        if (!resp.ok) return null;
        const data = await resp.json();
        return Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      } catch (e) {
        // try next candidate
      }
    }

    if (lastNon404 && !lastNon404.ok) return null;
    return null;
  }

  async function loadStudentLecturesToCalendar({ page = 0, pageSize = 200, fromMonthsAgo = null } = {}) {
    const data = await fetchStudentLectures({ page, pageSize, fromMonthsAgo });
    if (!data || data.length === 0) return;

    // If any active lecture exists, switch UI to the in-class panel
    try {
      const active = data.find(x => isActiveLectureStatus(x?.status ?? x?.Status));
      if (active) showInClass(active);
    } catch (e) { /* ignore */ }

    if (window.AttendanceCalendar && typeof window.AttendanceCalendar.addEvent === 'function') {
      for (const item of data) {
        const name = item?.name ?? item?.Name ?? 'Untitled';
        const startTime = item?.startTime ?? item?.StartTime;
        const description = item?.description ?? item?.Description ?? '';
        const duration = item?.duration ?? item?.Duration ?? null;
        const statusVal = item?.status ?? item?.Status;
        const evId = item?.id ?? item?.Id ?? null;

        // keep status map updated for the day pills
        try { if (evId != null && statusVal != null) window._lectureStatusMap[String(evId)] = statusVal; } catch (e) { /* ignore */ }

        const minutes = parseDurationToMinutes(duration);
        try {
          const startDate = startTime ? new Date(startTime) : null;
          const endDate = startDate ? new Date(startDate.getTime() + (minutes * 60 * 1000)) : null;
          if (startDate && endDate) {
            window.AttendanceCalendar.addEvent({
              id: evId,
              title: name,
              description: description || '',
              start: startDate,
              end: endDate,
              status: statusVal
            });
          }
        } catch (e) {
          console.error('Failed to add lecture to calendar', e, item);
        }
      }
    }
  }

  async function detectAndShowActiveLecture() {
    // Prefer querying only active/in-progress lectures for the UI toggle
    const data = await fetchStudentLectures({ page: 0, pageSize: 20, status: 'InProgress' });
    const active = Array.isArray(data) ? data.find(x => isActiveLectureStatus(x?.status ?? x?.Status)) : null;
    if (active) showInClass(active);
    else showJoin();
  }

  // Use the join page flow when student enters an ID
  function goToJoinPage() {
    const id = (classIdInput?.value ?? '').trim();
    if (!id) {
      try { classIdInput?.focus(); } catch (e) { /* ignore */ }
      return;
    }
    globalThis.location.href = `/lecture/join/${encodeURIComponent(id)}`;
  }

  joinBtn?.addEventListener('click', (e) => {
    e?.preventDefault?.();
    goToJoinPage();
  });

  classIdInput?.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      goToJoinPage();
    }
  });

  // Calendar month lazy-loading (same pattern as professor)
  (function ensureCalendarRegistration() {
    let checks = 0;
    const maxChecks = 200; // ~10s at 50ms
    const interval = setInterval(() => {
      checks++;
      if (window.AttendanceCalendar && typeof window.AttendanceCalendar.onViewChange === 'function') {
        clearInterval(interval);

        window.AttendanceCalendar.onViewChange(async (monthKey, viewDate) => {
          try {
            if (typeof window.AttendanceCalendar.isMonthLoaded === 'function' && window.AttendanceCalendar.isMonthLoaded(monthKey)) return;
            if (typeof window.AttendanceCalendar.markMonthLoading === 'function') window.AttendanceCalendar.markMonthLoading(monthKey);
            const now = new Date();
            const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) - (viewDate.getFullYear() * 12 + viewDate.getMonth());
            await loadStudentLecturesToCalendar({ fromMonthsAgo: monthsAgo });
            if (typeof window.AttendanceCalendar.markMonthLoaded === 'function') window.AttendanceCalendar.markMonthLoaded(monthKey);
          } catch (e) { console.error(e); }
        });

        // initial load for previous, current and next months
        (async () => {
          try {
            const now = new Date();
            const cur = new Date(now.getFullYear(), now.getMonth(), 1);
            const prev = new Date(cur.getFullYear(), cur.getMonth() - 1, 1);
            const next = new Date(cur.getFullYear(), cur.getMonth() + 1, 1);
            const months = [prev, cur, next];
            for (const d of months) {
              const monthKey = `${d.getFullYear()}-${d.getMonth() + 1}`;
              if (typeof window.AttendanceCalendar.isMonthLoaded === 'function' && window.AttendanceCalendar.isMonthLoaded(monthKey)) continue;
              if (typeof window.AttendanceCalendar.markMonthLoading === 'function') window.AttendanceCalendar.markMonthLoading(monthKey);
              const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) - (d.getFullYear() * 12 + d.getMonth());
              await loadStudentLecturesToCalendar({ fromMonthsAgo: monthsAgo });
              if (typeof window.AttendanceCalendar.markMonthLoaded === 'function') window.AttendanceCalendar.markMonthLoaded(monthKey);
            }
          } catch (e) { console.error(e); }
        })();
      }

      if (checks >= maxChecks) {
        clearInterval(interval);
      }
    }, 50);
  })();

  // Init UI state first
  joinState.hidden = true;
  inClassState.hidden = true;
  detectAndShowActiveLecture().catch(() => showJoin());
})();
