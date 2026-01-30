document.addEventListener('DOMContentLoaded', function () {
  const modalEl = document.getElementById('makeLectureModal');
  const form = document.getElementById('makeLectureForm');
  const feedbackEl = document.getElementById('makeLectureFeedback');
  if (!modalEl || !form || !feedbackEl) return;

  const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

  function showFeedback(msg, type) {
    feedbackEl.textContent = msg || '';
    feedbackEl.classList.remove('error', 'success');
    if (type === 'error') feedbackEl.classList.add('error');
    else if (type === 'success') feedbackEl.classList.add('success');
  }

  // When modal opens, close calendar popover if present and clear previous feedback
  modalEl.addEventListener('show.bs.modal', function () {
    try {
      showFeedback('', null);
      if (window.AttendanceCalendar && typeof window.AttendanceCalendar.close === 'function')
        window.AttendanceCalendar.close();
    } catch (e) { /* ignore */ }
  });

  // When modal is hidden, clear the form and reset submit state
  modalEl.addEventListener('hidden.bs.modal', function () {
    try {
      form.querySelector('[name="title"]').value = '';
      form.querySelector('[name="date"]').value = '';
      form.querySelector('[name="start"]').value = '';
      form.querySelector('[name="duration"]').value = '';
      form.querySelector('[name="description"]').value = '';
      const submitBtn = form.querySelector('[type="submit"]');
      if (submitBtn) submitBtn.disabled = false;
      showFeedback('', null);
    } catch (e) { /* ignore */ }
  });

  form.addEventListener('submit', async function (e) {
    e.preventDefault();

    const title = form.querySelector('[name="title"]').value.trim();
    const date = form.querySelector('[name="date"]').value; // YYYY-MM-DD
    const start = form.querySelector('[name="start"]').value; // HH:MM
    let duration = form.querySelector('[name="duration"]').value.trim();
    const description = form.querySelector('[name="description"]').value.trim();

    // Simple client-side validation
    if (!title || !date || !start || !duration || !description) {
      showFeedback('Please complete all fields.', 'error');
      return;
    }

    let startIso = null;
    try {
      const dt = new Date(date + 'T' + start);
      if (isNaN(dt.getTime())) throw new Error('Invalid date/time');
      startIso = dt.toISOString();
    } catch (err) {
      showFeedback('Invalid date or start time.', 'error');
      return;
    }

    // Parse duration input into TimeSpan format (c) -> HH:MM:SS
    function parseDurationToTimeSpan(input) {
      if (!input) return null;
      input = input.trim();

      // ISO 8601 duration (PT1H30M)
      if (/^P/i.test(input)) {
        try {
          const s = input.toUpperCase();
          const h = Number((s.match(/(\d+)H/) || [0, 0])[1] || 0);
          const m = Number((s.match(/(\d+)M/) || [0, 0])[1] || 0);
          const sec = Number((s.match(/(\d+)S/) || [0, 0])[1] || 0);
          const hh = String(h).padStart(2, '0');
          const mm = String(m).padStart(2, '0');
          const ss = String(sec).padStart(2, '0');
          return `${hh}:${mm}:${ss}`;
        } catch (e) { /* fallthrough */ }
      }

      // hh:mm
      if (/^\d+:\d+$/.test(input)) {
        const parts = input.split(':').map(Number);
        const hh = String(parts[0]).padStart(2, '0');
        const mm = String(parts[1]).padStart(2, '0');
        return `${hh}:${mm}:00`;
      }

      // 1h30m or 1h
      const hn = input.match(/^(\d+)h(?:([0-9]+)m)?$/i);
      if (hn) {
        const hh = String(Number(hn[1])).padStart(2, '0');
        const mm = String(Number(hn[2] || 0)).padStart(2, '0');
        return `${hh}:${mm}:00`;
      }

      // minutes like 90 or 90m
      const mn = input.match(/^(\d+)(?:m)?$/i);
      if (mn) {
        const totalMin = Number(mn[1]);
        const hh = String(Math.floor(totalMin / 60)).padStart(2, '0');
        const mm = String(totalMin % 60).padStart(2, '0');
        return `${hh}:${mm}:00`;
      }

      // already hh:mm:ss
      if (/^\d{1,2}:\d{2}:\d{2}$/.test(input)) return input;

      return null;
    }

    const durationTs = parseDurationToTimeSpan(duration);
    if (!durationTs) {
      showFeedback('Invalid duration format. See example formats.', 'error');
      return;
    }

    const payload = {
      name: title,
      description: description,
      startTime: startIso,
      duration: durationTs
    };

    const submitBtn = form.querySelector('[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;

    try {
      const resp = await fetch('/api/lectures', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify(payload)
      });

      if (!resp.ok) {
        let message = `Error ${resp.status}`;
        try {
          const data = await resp.json();
          if (data) {
            // Prefer the API detail field and extract between '--' and 'Severity:'
            if (data.detail) {
              const m = String(data.detail).match(/--\s*(.*?)\s*Severity:/is);
              if (m && m[1]) message = m[1].trim();
              else if (data.message) message = data.message;
              else if (data.title) message = data.title;
              else if (data.errors) message = JSON.stringify(data.errors);
              else message = String(data.detail).trim();
            }
            else if (data.message) message = data.message;
            else if (data.title) message = data.title;
            else if (data.errors) message = JSON.stringify(data.errors);
          }
        } catch (e) {
          try { message = await resp.text(); } catch (e) { /* ignore */ }
        }
        showFeedback(message, 'error');
        if (submitBtn) submitBtn.disabled = false;
        return;
      }

      // success — get created id from API and add event to calendar
      showFeedback('Created', 'success');
      try {
        const created = await resp.json();
        const createdId = (created && typeof created === 'string') ? created : (created?.Id || created);

        function timeSpanToMinutes(ts) {
          if (!ts) return 60;
          const parts = String(ts).split(':').map(Number);
          if (parts.length >= 2) return (Number(parts[0] || 0) * 60) + Number(parts[1] || 0) + Math.round(Number(parts[2] || 0) / 60);
          return parseInt(ts, 10) || 60;
        }

        const minutes = timeSpanToMinutes(durationTs);
        const startDate = startIso ? new Date(startIso) : null;
        const endDate = startDate ? new Date(startDate.getTime() + minutes * 60 * 1000) : null;
        if (window.AttendanceCalendar && typeof window.AttendanceCalendar.addEvent === 'function' && startDate && endDate) {
          try {
            window.AttendanceCalendar.addEvent({
              id: createdId || null,
              title: title,
              description: description || '',
              start: startDate,
              end: endDate
            });
          } catch (e) {
            console.error('Failed to add created lecture to calendar', e);
          }
        }
      } catch (e) {
        // ignore JSON parse errors
      }

      // close after 1 second
      setTimeout(() => modal.hide(), 1000);
    } catch (err) {
      console.error(err);
      showFeedback('Network error', 'error');
      if (submitBtn) submitBtn.disabled = false;
    }
  });

  // Load professor lectures and add them to the calendar (no UI text updates)
  async function loadProfessorLectures({ page = 0, pageSize = 200, fromMonthsAgo = null } = {}) {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    if (fromMonthsAgo != null) params.set('fromMonthsAgo', String(fromMonthsAgo));

    try {
      const resp = await fetch('/api/lectures/me?' + params.toString(), {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!resp.ok) {
        return;
      }

      const data = await resp.json();
      if (!data || data.length === 0) {
        return;
      }

      // Render active lecture quadrants under the professor dashboard
      try {
        for (const item of data) {
          cacheLectureMeta(item);
          const id = item?.id ?? item?.Id;
          const status = item?.status ?? item?.Status;
          if (isActiveLectureStatus(status)) upsertActiveLectureCard(item);
          else if (id != null) removeActiveLectureCard(id);
        }

        // Immediately refresh attendee totals (then keep polling every 3s)
        try { pollActiveLectureAttendeeCounts(); } catch (e) { /* ignore */ }
      } catch (e) {
        console.error('Failed to render active lecture cards', e);
      }

      // Add lectures to the calendar using existing API
      if (window.AttendanceCalendar && typeof window.AttendanceCalendar.addEvent === 'function') {
        function parseDurationToMinutes(input) {
          if (input == null) return 60;
          if (typeof input === 'number') return Math.max(0, Math.floor(input));
          const s = String(input).trim();
          if (/^P/i.test(s)) {
            const up = s.toUpperCase();
            const h = Number((up.match(/(\d+)H/) || [0,0])[1] || 0);
            const m = Number((up.match(/(\d+)M/) || [0,0])[1] || 0);
            const sec = Number((up.match(/(\d+)S/) || [0,0])[1] || 0);
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

        for (const item of data) {
          const name = item.name || item.Name || 'Untitled';
          const startTime = item.startTime || item.StartTime;
          const description = item.description || item.Description || '';
          const duration = item.duration || item.Duration || null;

          const minutes = parseDurationToMinutes(duration);
          try {
            const startDate = startTime ? new Date(startTime) : null;
            const endDate = startDate ? new Date(startDate.getTime() + (minutes * 60 * 1000)) : null;
            const evId = item.id || item.Id || null;
            if (startDate && endDate) {
                window.AttendanceCalendar.addEvent({
                  id: evId,
                  title: name,
                  description: description || '',
                  start: startDate,
                  end: endDate,
                  status: item.status ?? item.Status
                });
            }
          } catch (e) {
            console.error('Failed to add lecture to calendar', e, item);
          }
        }
      }
    } catch (err) {
      console.error(err);
    }
  }

  // Basic HTML escaper
  function escapeHtml(s) {
    return String(s)
      .replace(/&/g, '&amp;') 
      .replace(/</g, '&lt;') 
      .replace(/>/g, '&gt;') 
      .replace(/"/g, '&quot;') 
      .replace(/'/g, '&#039;'); 
  }

  // ------------------ Active lecture quadrants under dashboard ------------------
  const activeLecturesHost = document.getElementById('professorActiveLectures');
  const activeLectureCardsById = new Map();
  const lectureMetaById = new Map();

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

  function getJoinUrl(lectureId) {
    try {
      return new URL(`/lecture/join/${encodeURIComponent(String(lectureId))}`, window.location.origin).toString();
    } catch (e) {
      return `/lecture/join/${encodeURIComponent(String(lectureId))}`;
    }
  }

  function cacheLectureMeta(item) {
    try {
      const id = item?.id ?? item?.Id;
      if (!id) return;
      const key = String(id);
      const name = item?.name ?? item?.Name;
      const description = item?.description ?? item?.Description;
      if (name != null || description != null) {
        lectureMetaById.set(key, {
          name: name ?? lectureMetaById.get(key)?.name,
          description: description ?? lectureMetaById.get(key)?.description
        });
      }
    } catch (e) { /* ignore */ }
  }

  function ensureQr(el, urlText) {
    if (!el) return;
    try { el.innerHTML = ''; } catch (e) { /* ignore */ }

    // Prefer local QRCode generator if present
    if (window.QRCode) {
      try {
        // eslint-disable-next-line no-new
        new window.QRCode(el, {
          text: urlText,
          width: 256,
          height: 256,
          correctLevel: window.QRCode.CorrectLevel ? window.QRCode.CorrectLevel.M : undefined
        });
        return;
      } catch (e) { /* fallthrough */ }
    }

    // Fallback: public QR image generator (keeps UI working even if QRCode lib fails to load)
    try {
      const img = document.createElement('img');
      img.alt = 'QR code';
      img.loading = 'lazy';
      img.referrerPolicy = 'no-referrer';
      img.src = `https://api.qrserver.com/v1/create-qr-code/?size=256x256&data=${encodeURIComponent(urlText)}`;
      el.appendChild(img);
    } catch (e) { /* ignore */ }
  }

  function upsertActiveLectureCard(item) {
    if (!activeLecturesHost) return;
    const lectureId = item?.id ?? item?.Id;
    if (!lectureId) return;

    const key = String(lectureId);
    cacheLectureMeta(item);
    let card = activeLectureCardsById.get(key);

    if (!card) {
      card = document.createElement('div');
      card.className = 'professor-home professor-lecture-card';
      card.dataset.lectureId = key;
      card.innerHTML = `
        <h4 class="pl-title"></h4>
        <p class="pl-desc"></p>
        <div class="pl-qr-wrap"><div class="pl-qr"></div></div>
        <div class="pl-id"></div>
        <div class="pl-attendees">
          <div class="pl-att-count" aria-live="polite">Attendees: —</div>
          <button type="button" class="btn-inline pl-att-btn">Attendees</button>
        </div>
      `;
      activeLecturesHost.appendChild(card);
      activeLectureCardsById.set(key, card);

      // Wire attendee popover button
      const btn = card.querySelector('.pl-att-btn');
      if (btn) {
        btn.addEventListener('click', (e) => {
          e.preventDefault();
          e.stopPropagation();
          openAttendeesPopover(key, btn);
        });
      }
    }

    const cached = lectureMetaById.get(key);
    const title = item?.name ?? item?.Name ?? cached?.name ?? 'Untitled';
    const description = item?.description ?? item?.Description ?? cached?.description ?? '';
    const joinUrl = getJoinUrl(key);

    const titleEl = card.querySelector('.pl-title');
    const descEl = card.querySelector('.pl-desc');
    const qrEl = card.querySelector('.pl-qr');
    const idEl = card.querySelector('.pl-id');

    if (titleEl) titleEl.textContent = String(title);
    if (descEl) descEl.textContent = String(description);
    if (idEl) idEl.textContent = `Lecture ID (join): ${key}`;
    ensureQr(qrEl, joinUrl);
  }

  function removeActiveLectureCard(lectureId) {
    if (!lectureId) return;
    const key = String(lectureId);
    const card = activeLectureCardsById.get(key);
    if (card && card.parentNode) {
      try { card.parentNode.removeChild(card); } catch (e) { /* ignore */ }
    }
    activeLectureCardsById.delete(key);

    // If popover is currently showing this lecture, close it
    try {
      if (attendeesPopoverState?.lectureId === key) closeAttendeesPopover();
    } catch (e) { /* ignore */ }
  }

  // ------------------ Active lecture attendee totals + attendee list popover ------------------
  const attendeeTotalsInFlight = new Set();

  async function fetchLectureAttendeesPage(lectureId, page, pageSize) {
    const candidates = [
      `/api/lectureAttendees/${encodeURIComponent(String(lectureId))}?page=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`,
      `/api/lectureAttendees/${encodeURIComponent(String(lectureId))}?pageNumber=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`,
    ];

    for (const url of candidates) {
      try {
        const resp = await fetch(url, {
          method: 'GET',
          credentials: 'same-origin',
          headers: { 'Accept': 'application/json' }
        });

        if (resp.status === 404) continue;
        if (!resp.ok) return null;

        const data = await resp.json();
        const items = data?.items ?? data?.Items ?? [];
        const total = data?.total ?? data?.Total ?? 0;
        return { items: Array.isArray(items) ? items : [], total: Number(total || 0) };
      } catch (e) {
        // try next
      }
    }
    return null;
  }

  async function fetchUserInfoBatch(ids) {
    const distinct = Array.from(new Set((ids || []).filter(Boolean).map(String)));
    if (distinct.length === 0) return new Map();

    const qs = new URLSearchParams();
    for (const id of distinct) qs.append('ids', id);

    try {
      let data = null;
      const resp = await fetch(`/api/users/userInfo?${qs.toString()}`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });
      if (!resp.ok) return new Map();
      data = await resp.json();

      if (!data) return new Map();
      const arr = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      const map = new Map();
      for (const u of arr) {
        const id = u?.id ?? u?.Id;
        if (!id) continue;
        map.set(String(id), {
          name: u?.name ?? u?.Name ?? '',
          email: u?.email ?? u?.Email ?? ''
        });
      }
      return map;
    } catch (e) {
      return new Map();
    }
  }

  async function pollActiveLectureAttendeeCounts() {
    if (!activeLecturesHost) return;
    const ids = Array.from(activeLectureCardsById.keys());
    if (ids.length === 0) return;

    for (const lectureId of ids) {
      if (attendeeTotalsInFlight.has(lectureId)) continue;
      attendeeTotalsInFlight.add(lectureId);
      (async () => {
        try {
          const page = await fetchLectureAttendeesPage(lectureId, 0, 1);
          const total = page?.total;
          const card = activeLectureCardsById.get(String(lectureId));
          if (card) {
            const el = card.querySelector('.pl-att-count');
            if (el && typeof total === 'number' && !Number.isNaN(total)) el.textContent = `Attendees: ${total}`;
          }

          // If popover is open for this lecture, update its header total
          try {
            if (attendeesPopoverState?.lectureId === String(lectureId) && attendeesPopoverState?.totalEl) {
              if (typeof total === 'number' && !Number.isNaN(total)) {
                attendeesPopoverState.totalKnown = total;
                updateAttendeesPopoverHeader();
                updateAttendeesLoadMoreVisibility();
              }
            }
          } catch (e) { /* ignore */ }
        } finally {
          attendeeTotalsInFlight.delete(lectureId);
        }
      })();
    }
  }

  // Poll every 60 seconds
  try {
    setInterval(pollActiveLectureAttendeeCounts, 60000);
  } catch (e) { /* ignore */ }

  // ---- Popover state + UI ----
  let attendeesPopoverEl = null;
  let attendeesPopoverState = null;

  function updateAttendeesLoadMoreVisibility(lastPageItemCount = null) {
    if (!attendeesPopoverState) return;
    const { loadMoreBtn, loadedCount, totalKnown, pageSize } = attendeesPopoverState;
    if (!loadMoreBtn) return;

    const totalIsKnown = typeof totalKnown === 'number' && !Number.isNaN(totalKnown);
    const doneByTotal = totalIsKnown && loadedCount >= totalKnown;
    const doneByPage = typeof lastPageItemCount === 'number' && lastPageItemCount < pageSize;
    const done = doneByTotal || doneByPage;

    loadMoreBtn.style.display = done ? 'none' : 'inline-flex';
    if (done) loadMoreBtn.disabled = true;
  }

  function ensureAttendeesPopover() {
    if (attendeesPopoverEl) return attendeesPopoverEl;

    const pop = document.createElement('div');
    pop.id = 'attendeesPopover';
    pop.className = 'att-popover';
    pop.style.display = 'none';
    pop.innerHTML = `
      <div class="att-head">
        <div class="att-title">Attendees</div>
        <button type="button" class="att-close" aria-label="Close">×</button>
      </div>
      <div class="att-sub" aria-live="polite"></div>
      <div class="att-list" role="list"></div>
      <button type="button" class="att-load-more" aria-label="Load more">+</button>
    `;
    document.body.appendChild(pop);

    const closeBtn = pop.querySelector('.att-close');
    if (closeBtn) closeBtn.addEventListener('click', (e) => { e.preventDefault(); closeAttendeesPopover(); });

    // click inside shouldn't close
    pop.addEventListener('click', (e) => e.stopPropagation());

    // close on outside click
    document.addEventListener('click', (e) => {
      if (!attendeesPopoverEl || attendeesPopoverEl.style.display === 'none') return;
      if (!attendeesPopoverEl.contains(e.target)) closeAttendeesPopover();
    });

    // close on escape
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') closeAttendeesPopover();
    });

    attendeesPopoverEl = pop;
    return pop;
  }

  function positionAttendeesPopover(anchorEl) {
    if (!attendeesPopoverEl || !anchorEl) return;
    attendeesPopoverEl.style.position = 'fixed';
    attendeesPopoverEl.style.zIndex = 22000;

    const r = anchorEl.getBoundingClientRect();
    const margin = 10;
    const desiredTop = r.bottom + 8;
    const desiredLeft = r.left;

    // Temporarily show to measure
    attendeesPopoverEl.style.display = 'block';
    const pw = attendeesPopoverEl.offsetWidth;
    const ph = attendeesPopoverEl.offsetHeight;
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    let top = desiredTop;
    let left = desiredLeft;

    if (left + pw + margin > vw) left = Math.max(margin, vw - pw - margin);
    if (left < margin) left = margin;

    if (top + ph + margin > vh) {
      // try above the anchor
      top = r.top - ph - 8;
      if (top < margin) top = Math.max(margin, vh - ph - margin);
    }

    attendeesPopoverEl.style.top = `${top}px`;
    attendeesPopoverEl.style.left = `${left}px`;
  }

  function closeAttendeesPopover() {
    if (!attendeesPopoverEl) return;
    attendeesPopoverEl.style.display = 'none';
    attendeesPopoverState = null;
  }

  function updateAttendeesPopoverHeader() {
    if (!attendeesPopoverState) return;
    const { subEl, loadedCount, totalKnown } = attendeesPopoverState;
    if (!subEl) return;
    const totalTxt = (typeof totalKnown === 'number' && !Number.isNaN(totalKnown)) ? totalKnown : '—';
    subEl.textContent = `Loaded ${loadedCount} / ${totalTxt}`;
  }

  async function loadMoreAttendees() {
    if (!attendeesPopoverState || attendeesPopoverState.loading) return;
    attendeesPopoverState.loading = true;
    try {
      const { lectureId, nextPage, pageSize, listEl, loadMoreBtn } = attendeesPopoverState;
      if (!lectureId || !listEl) return;

      if (loadMoreBtn) loadMoreBtn.disabled = true;

      const page = await fetchLectureAttendeesPage(lectureId, nextPage, pageSize);
      if (!page) return;

      if (typeof page.total === 'number' && !Number.isNaN(page.total)) attendeesPopoverState.totalKnown = page.total;

      const rawItems = Array.isArray(page.items) ? page.items : [];
      const items = rawItems.slice().sort((a, b) => {
        const ta = a?.timeJoined ?? a?.TimeJoined;
        const tb = b?.timeJoined ?? b?.TimeJoined;
        const da = ta ? Date.parse(ta) : NaN;
        const db = tb ? Date.parse(tb) : NaN;
        if (!Number.isNaN(da) && !Number.isNaN(db) && da !== db) return da - db;
        const ua = String(a?.userId ?? a?.UserId ?? '');
        const ub = String(b?.userId ?? b?.UserId ?? '');
        return ua.localeCompare(ub);
      });

      if (items.length === 0 && attendeesPopoverState.loadedCount === 0) {
        if (attendeesPopoverState.subEl) attendeesPopoverState.subEl.textContent = 'No attendees yet.';
        updateAttendeesLoadMoreVisibility(0);
        return;
      }

      const userIds = items.map(x => x?.userId ?? x?.UserId).filter(Boolean);
      const userMap = await fetchUserInfoBatch(userIds);

      let added = 0;
      for (const it of items) {
        const uid = String(it?.userId ?? it?.UserId ?? '');
        if (!uid) continue;
        const joinedRaw = it?.timeJoined ?? it?.TimeJoined;
        let joinedText = '';
        try {
          joinedText = joinedRaw ? new Date(joinedRaw).toLocaleString() : '';
        } catch (e) { joinedText = ''; }

        const info = userMap.get(uid);
        const name = info?.name || uid;
        const email = info?.email || '';

        const row = document.createElement('div');
        row.className = 'att-item';
        row.innerHTML = `
          <div class="att-left">
            <div class="att-name">${escapeHtml(name)}</div>
            <div class="att-email">${escapeHtml(email)}</div>
          </div>
          <div class="att-time">${escapeHtml(joinedText)}</div>
        `;
        listEl.appendChild(row);
        attendeesPopoverState.loadedCount++;
        added++;
      }

      attendeesPopoverState.nextPage = nextPage + 1;
      updateAttendeesPopoverHeader();

      updateAttendeesLoadMoreVisibility(added);
    } finally {
      if (attendeesPopoverState) {
        attendeesPopoverState.loading = false;
        if (attendeesPopoverState.loadMoreBtn && attendeesPopoverState.loadMoreBtn.style.display !== 'none') {
          attendeesPopoverState.loadMoreBtn.disabled = false;
        }
      }
    }
  }

  function openAttendeesPopover(lectureId, anchorEl) {
    const pop = ensureAttendeesPopover();
    const listEl = pop.querySelector('.att-list');
    const subEl = pop.querySelector('.att-sub');
    const loadMoreBtn = pop.querySelector('.att-load-more');

    if (listEl) listEl.innerHTML = '';
    if (subEl) subEl.textContent = 'Loading…';
    if (loadMoreBtn) {
      loadMoreBtn.disabled = false;
      loadMoreBtn.style.display = 'inline-flex';
      loadMoreBtn.onclick = (e) => { e.preventDefault(); loadMoreAttendees(); };
    }

    attendeesPopoverState = {
      lectureId: String(lectureId),
      nextPage: 0,
      pageSize: 20,
      loadedCount: 0,
      totalKnown: null,
      loading: false,
      listEl,
      subEl,
      loadMoreBtn,
      totalEl: subEl
    };

    updateAttendeesPopoverHeader();

    pop.style.display = 'block';
    positionAttendeesPopover(anchorEl);

    // Load first page (top to bottom)
    loadMoreAttendees();
  }

  function updateRenderedCalendarStatusPill(lectureId, statusVal) {
    if (!lectureId) return;
    try {
      const raw = String(lectureId);
      const esc = (typeof CSS !== 'undefined' && CSS.escape) ? CSS.escape(raw) : raw.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
      const pills = document.querySelectorAll(`.event-status-pill[data-ev-id="${esc}"]`);
      if (!pills || pills.length === 0) return;

      let label = '';
      const lower = String(statusVal).toLowerCase();
      let cls = null;
      if (lower === 'scheduled' || statusVal === 0) { cls = 'status-scheduled'; label = 'Scheduled'; }
      else if (lower === 'inprogress' || statusVal === 1) { cls = 'status-inprogress'; label = 'InProgress'; }
      else if (lower === 'ended' || statusVal === 2) { cls = 'status-ended'; label = 'Ended'; }
      else if (lower === 'canceled' || statusVal === 3) { cls = 'status-canceled'; label = 'Canceled'; }

      pills.forEach((pill) => {
        pill.classList.remove('status-scheduled', 'status-inprogress', 'status-ended', 'status-canceled');
        if (cls) pill.classList.add(cls);
        pill.textContent = label;
      });
    } catch (e) { /* ignore */ }
  }

  // register month-change loader with AttendanceCalendar once it is available
  (function ensureCalendarRegistration() {
    let checks = 0;
    const maxChecks = 200; // ~10s at 50ms
    const interval = setInterval(() => {
      checks++;
      if (window.AttendanceCalendar && typeof window.AttendanceCalendar.onViewChange === 'function') {
        clearInterval(interval);

        // register loader for view changes
        window.AttendanceCalendar.onViewChange(async (monthKey, viewDate) => {
          try {
            if (typeof window.AttendanceCalendar.isMonthLoaded === 'function' && window.AttendanceCalendar.isMonthLoaded(monthKey)) return;
            // mark as loading to prevent duplicate concurrent loads
            if (typeof window.AttendanceCalendar.markMonthLoading === 'function') window.AttendanceCalendar.markMonthLoading(monthKey);
            const now = new Date();
            const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) - (viewDate.getFullYear() * 12 + viewDate.getMonth());
            await loadProfessorLectures({ fromMonthsAgo: monthsAgo });
            if (typeof window.AttendanceCalendar.markMonthLoaded === 'function') window.AttendanceCalendar.markMonthLoaded(monthKey);
          } catch (e) { console.error(e); }
        });

        // initial load for previous, current and next months so days visible in the grid have events
        (async () => {
          try {
            const now = new Date();
            const cur = new Date(now.getFullYear(), now.getMonth(), 1);
            const prev = new Date(cur.getFullYear(), cur.getMonth() - 1, 1);
            const next = new Date(cur.getFullYear(), cur.getMonth() + 1, 1);
            const months = [prev, cur, next];
            for (const d of months) {
              const monthKey = `${d.getFullYear()}-${d.getMonth()+1}`;
              if (typeof window.AttendanceCalendar.isMonthLoaded === 'function' && window.AttendanceCalendar.isMonthLoaded(monthKey)) continue;
              if (typeof window.AttendanceCalendar.markMonthLoading === 'function') window.AttendanceCalendar.markMonthLoading(monthKey);
              const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) - (d.getFullYear() * 12 + d.getMonth());
              await loadProfessorLectures({ fromMonthsAgo: monthsAgo });
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

  // ------------------ Lecture action modal handler ------------------
  // Map to track lecture statuses by id
  window._lectureStatusMap = window._lectureStatusMap || {};

  // Expose handler for calendar event clicks — show small popup above the calendar event element
  window.onCalendarEventClick = function(ev) {
    try {
      // find the DOM node for this event
      const el = document.querySelector(`[data-event-id="${ev.id}"]`);
      // create popup if needed
      let popup = document.getElementById('lecturePopup');
      if (!popup) {
        popup = document.createElement('div');
        popup.id = 'lecturePopup';
        popup.className = 'lecture-popup';
        popup.innerHTML = `
          <button type="button" class="lp-close" aria-label="Close">×</button>
          <div class="lp-body">
            <div class="lp-title"></div>
            <div class="lp-time small text-muted"></div>
            <div class="lp-row" style="display:flex;align-items:center;gap:8px;">
              <div class="lp-desc small" style="flex:1"></div>
            </div>
            <div class="lp-status small mt-1"></div>
            <div class="lp-actions mt-2"></div>
          </div>
        `;
        document.body.appendChild(popup);
        // close on clicking the close button
        const closeBtn = popup.querySelector('.lp-close');
        if (closeBtn) {
          closeBtn.addEventListener('click', (ev) => { ev.stopPropagation(); try { popup.style.display = 'none'; } catch(e){} });
        }
        // stop clicks inside popup from bubbling up (so calendar/popover doesn't close)
        popup.addEventListener('click', (ev) => { ev.stopPropagation(); });
        // close on outside click
        document.addEventListener('click', (e) => {
          if (!popup.contains(e.target)) popup.style.display = 'none';
        });
      }

      const titleEl = popup.querySelector('.lp-title');
      const timeEl = popup.querySelector('.lp-time');
      const descEl = popup.querySelector('.lp-desc');
      const statusEl = popup.querySelector('.lp-status');
      const actionsEl = popup.querySelector('.lp-actions');

      titleEl.textContent = ev.title || '';
      descEl.textContent = ev.description || '';
      timeEl.textContent = `${new Date(ev.start).toLocaleString()} — ${new Date(ev.end).toLocaleString()}`;

      const id = ev.id;
      const current = window._lectureStatusMap && window._lectureStatusMap[id] !== undefined ? window._lectureStatusMap[id] : (ev.status ?? ev.Status);
      statusEl.textContent = `Status: ${current ?? 'Unknown'}`;




      // clear previous action buttons so they don't accumulate
      try { actionsEl.innerHTML = ''; } catch (e) { /* ignore */ }

      function makeBtn(label, cls, fn) {
        const b = document.createElement('button');
        b.type = 'button';
        b.className = cls || 'btn-inline';
        b.textContent = label;
        b.addEventListener('click', fn);
        return b;
      }

      const curLower = (String(current) || '').toLowerCase();
      if (curLower === 'scheduled' || current === 0) {
        actionsEl.appendChild(makeBtn('Start', 'btn-inline', async () => await doStatusChange(id, 1, popup)));
        actionsEl.appendChild(makeBtn('Cancel', 'btn-inline', async () => await doStatusChange(id, 3, popup)));
      } else if (curLower === 'inprogress' || current === 1) {
        actionsEl.appendChild(makeBtn('End', 'btn-inline', async () => await doStatusChange(id, 2, popup)));
      }

      // position popup above event element or near calendar popover
      popup.style.display = 'block';
      popup.style.position = 'fixed';
      popup.style.zIndex = 20000;
      if (el) {
        const r = el.getBoundingClientRect();
        const top = r.top - popup.offsetHeight - 8;
        const left = r.left + (r.width - popup.offsetWidth) / 2;
        popup.style.top = `${Math.max(8, top)}px`;
        popup.style.left = `${Math.max(8, left)}px`;
      } else {
        // fallback: position near calendar popover
        const pop = document.getElementById('calendarPopover');
        if (pop) {
          const pr = pop.getBoundingClientRect();
          const top = pr.top - popup.offsetHeight - 8;
          const left = pr.left + (pr.width - popup.offsetWidth) / 2;
          popup.style.top = `${Math.max(8, top)}px`;
          popup.style.left = `${Math.max(8, left)}px`;
        }
      }
    } catch (e) { console.error(e); }
  };

  async function doStatusChange(lectureId, statusValue, popupOrFeedback, modalEl) {
    if (!lectureId) return;
    // popupOrFeedback may be the popup element (lecturePopup) or a feedback element inside a modal
    const popup = (popupOrFeedback && popupOrFeedback.id === 'lecturePopup') ? popupOrFeedback : null;
    const feedbackEl = popup ? (popup.querySelector('.lp-actions') || popup) : popupOrFeedback;

    try {
      const resp = await fetch(`/api/lectures/status/${lectureId}`, {
        method: 'PUT',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: statusValue })
      });

      if (!resp.ok) {
        let txt = `Error ${resp.status}`;
        try { const j = await resp.json(); if (j && j.detail) txt = j.detail; } catch (e) { try { txt = await resp.text(); } catch {} }
        try { if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') feedbackEl.textContent = txt; }
        catch (e) { /* ignore */ }
        return;
      }

      // success: update local status map
      let updated = null;
      try { updated = await resp.json(); } catch (e) { /* ok - endpoint may return no content */ }
      const newStatus = updated?.status ?? updated?.Status ?? statusValue;
      window._lectureStatusMap[lectureId] = newStatus;

      // update any already-rendered status pill in the calendar
      updateRenderedCalendarStatusPill(lectureId, newStatus);

      // keep active lecture cards in sync
      try {
        // prefer cached lecture name/description if endpoint doesn't return them
        const meta = lectureMetaById.get(String(lectureId));
        if (isActiveLectureStatus(newStatus)) {
          upsertActiveLectureCard({
            id: lectureId,
            name: updated?.name ?? updated?.Name ?? meta?.name,
            description: updated?.description ?? updated?.Description ?? meta?.description,
            status: newStatus
          });
        } else {
          removeActiveLectureCard(lectureId);
        }
      } catch (e) { /* ignore */ }

      // hide popup or modal depending on caller
      if (modalEl) {
        try {
          const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
          bsModal.hide();
        } catch (e) { /* ignore */ }
      } else if (popup) {
        try { popup.style.display = 'none'; } catch (e) { /* ignore */ }
      }
    } catch (e) {
      console.error(e);
      try { if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') feedbackEl.textContent = 'Network error'; } catch (e) { /* ignore */ }
    }
  }
});
