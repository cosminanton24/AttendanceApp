(function(){
  const btn = document.getElementById("calendarBtn");
  const pop = document.getElementById("calendarPopover");

  if (!btn || !pop) {
    console.warn("[AttendanceCalendar] Missing #calendarBtn or #calendarPopover");
    return;
  }

  // Prevent clicks inside popover from closing it
  pop.addEventListener("click", (e) => e.stopPropagation());

  const elMonth = pop.querySelector("[data-cal-month]");
  const elGrid = pop.querySelector("[data-cal-grid]");
  const elDayLabel = pop.querySelector("[data-cal-daylabel]");
  const elHours = pop.querySelector("[data-cal-hours]");
  const elEvents = pop.querySelector("[data-cal-events]");

  // Add mobile day navigation buttons into the day header
const dayHead = pop.querySelector(".cal-day-head");
if (dayHead && !dayHead.querySelector(".cal-day-nav")) {
  const nav = document.createElement("div");
  nav.className = "cal-day-nav";
  nav.innerHTML = `
    <button type="button" class="cal-day-btn" data-day-prev aria-label="Previous day">‹</button>
    <button type="button" class="cal-day-btn" data-day-next aria-label="Next day">›</button>
  `;
  dayHead.appendChild(nav);

  nav.querySelector("[data-day-prev]").addEventListener("click", (e) => {
    e.stopPropagation();
    state.selected = addDays(state.selected, -1);
    // keep month in sync for desktop
    state.view = new Date(state.selected.getFullYear(), state.selected.getMonth(), 1);
    renderMonth();
    renderDay();
  });

  nav.querySelector("[data-day-next]").addEventListener("click", (e) => {
    e.stopPropagation();
    state.selected = addDays(state.selected, 1);
    state.view = new Date(state.selected.getFullYear(), state.selected.getMonth(), 1);
    renderMonth();
    renderDay();
  });
}


  if (!elMonth || !elGrid || !elDayLabel || !elHours || !elEvents) {
    console.warn("[AttendanceCalendar] Missing one or more [data-cal-*] elements");
    return;
  }

  const state = {
    view: new Date(),      // month being viewed
    selected: new Date(),  // selected day
    events: [],            // {id,title,description,start:Date,end:Date}
    onOpenCb: null,
    // loadedMonths stores keys like 'YYYY-M' to prevent redundant loads
    loadedMonths: new Set(),
    // loadingMonths tracks in-flight month loads to avoid duplicate concurrent requests
    loadingMonths: new Set(),
    onViewChange: null
  };

  // --- helpers ---
  let nowLineEl = null;

function renderNowLine(){
  const today = new Date();
  if (!sameDay(state.selected, today)) {
    if (nowLineEl) nowLineEl.remove();
    nowLineEl = null;
    return;
  }
  if (!elEvents) return;

  const ppm = getPPM();
  const minutesNow = today.getHours() * 60 + today.getMinutes();
  const y = minutesNow * ppm;

  if (!nowLineEl) {
    nowLineEl = document.createElement("div");
    nowLineEl.className = "cal-now";
  }

  nowLineEl.style.top = `${y}px`;

  elEvents.appendChild(nowLineEl);
}


  const pad2 = n => String(n).padStart(2, "0");
  const sameDay = (a, b) =>
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate();

  const clamp = (x, min, max) => Math.max(min, Math.min(max, x));

  function formatMonth(d){
    return d.toLocaleString(undefined, { month: "long", year: "numeric" });
  }

  function formatDayLabel(d){
    return d.toLocaleString(undefined, { weekday:"long", year:"numeric", month:"short", day:"numeric" });
  }

  function formatTime(d){
    return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
  }

  function escapeHtml(s){
    return String(s ?? "")
      .replaceAll("&","&amp;")
      .replaceAll("<","&lt;")
      .replaceAll(">","&gt;")
      .replaceAll('"',"&quot;")
      .replaceAll("'","&#039;");
  }

  function getPPM(){
    // Read --ppm from .cal-events (inherited from .cal-timeline)
    const ppmRaw = getComputedStyle(elEvents).getPropertyValue("--ppm").trim();
    const ppm = Number.parseFloat(ppmRaw);
    return Number.isFinite(ppm) && ppm > 0 ? ppm : 1.25;
  }

  function openPopover(){
    pop.classList.add("open");
    pop.setAttribute("aria-hidden", "false");
    if (typeof state.onOpenCb === "function") state.onOpenCb();
    scrollToNowIfToday();
  }

  function scrollToNowIfToday(){
  const today = new Date();
  if (!sameDay(state.selected, today)) return;

  const ppm = getPPM();
  const minutesNow = today.getHours() * 60 + today.getMinutes();
  const targetY = minutesNow * ppm;

  const scroller = elEvents.parentElement; // .cal-timeline
  scroller.scrollTop = Math.max(0, targetY - 120);
}

  function closePopover(){
    pop.classList.remove("open");
    pop.setAttribute("aria-hidden", "true");
  }

  function togglePopover(){
    pop.classList.contains("open") ? closePopover() : openPopover();
  }

  function addDays(date, delta){
  const d = new Date(date);
  d.setDate(d.getDate() + delta);
  return d;
}

  // Close on outside click
  document.addEventListener("click", (e) => {
    if (!pop.classList.contains("open")) return;
    if (pop.contains(e.target) || btn.contains(e.target)) return;
    closePopover();
  });

  // --- month render ---
  function overlapsDay(ev, day){
    const start = new Date(ev.start);
    const end = new Date(ev.end);
    const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0,0,0,0);
    const dayEnd   = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 23,59,59,999);
    return start <= dayEnd && end >= dayStart;
  }

  function renderMonth(){
    elMonth.textContent = formatMonth(state.view);
    elGrid.innerHTML = "";

    const year = state.view.getFullYear();
    const month = state.view.getMonth();

    const first = new Date(year, month, 1);
    const firstDowMon = (first.getDay() + 6) % 7; // Mon=0..Sun=6
    const start = new Date(year, month, 1 - firstDowMon);

    for (let i = 0; i < 42; i++){
      const d = new Date(start);
      d.setDate(start.getDate() + i);

      const cell = document.createElement("div");
      cell.className = "cal-cell";
      if (d.getMonth() !== month) cell.classList.add("muted");
      if (sameDay(d, state.selected)) cell.classList.add("selected");

      const dayNum = document.createElement("div");
      dayNum.className = "cal-daynum";
      dayNum.textContent = d.getDate();

      const hasEvents = state.events.some(ev => overlapsDay(ev, d));
        if (hasEvents) {
        const dot = document.createElement("span");
        dot.className = "cal-dot";
        cell.appendChild(dot);
        }

      cell.appendChild(dayNum);

      cell.addEventListener("click", (e) => {
        e.stopPropagation(); // keep popover open
        state.selected = new Date(d.getFullYear(), d.getMonth(), d.getDate());
        renderMonth();
        renderDay();
      });

      elGrid.appendChild(cell);
    }

    // notify listener that the view changed (for lazy-loading events)
    try {
      // load previous, current and next months because the month grid shows days
      const monthsToCheck = [];
      const cur = new Date(state.view.getFullYear(), state.view.getMonth(), 1);
      const prev = new Date(cur.getFullYear(), cur.getMonth() - 1, 1);
      const next = new Date(cur.getFullYear(), cur.getMonth() + 1, 1);
      monthsToCheck.push(prev, cur, next);

      for (const d of monthsToCheck) {
        const monthKey = `${d.getFullYear()}-${d.getMonth()+1}`;
        if (typeof state.onViewChange === 'function' && !state.loadedMonths.has(monthKey) && !state.loadingMonths.has(monthKey)) {
          state.loadingMonths.add(monthKey);
          try { state.onViewChange(monthKey, new Date(d)); } catch (e) { console.error(e); }
        }
      }
    } catch (e) { /* ignore */ }
  }

  // --- day render (Outlook-ish) ---
  function renderDay(){
    elDayLabel.textContent = formatDayLabel(state.selected);

    elHours.innerHTML = "";
    elEvents.innerHTML = "";

    const ppm = getPPM();
    const minutesInDay = 24 * 60;

    // Hour labels every 2 hours
    for (let h = 0; h < 24; h += 1){
      const label = document.createElement("div");
      label.className = "cal-hour";
      label.textContent = `${pad2(h)}:00`;
      label.style.top = `${h * 60 * ppm}px`;
      if (h === 0) label.style.transform = "translateY(6px)";
      if (h % 2 === 1) label.classList.add("odd");
      elHours.appendChild(label);
    }

    // Events for selected day
    const dayEvents = state.events
      .filter(ev => overlapsDay(ev, state.selected))
      .sort((a, b) => a.start - b.start);

    dayEvents.forEach(ev => {
      const block = document.createElement("div");
      block.className = "cal-event";

      const dayStart = new Date(state.selected.getFullYear(), state.selected.getMonth(), state.selected.getDate(), 0,0,0,0);
      const dayEnd = new Date(state.selected.getFullYear(), state.selected.getMonth(), state.selected.getDate(), 23,59,59,999);

      const start = new Date(Math.max(ev.start.getTime(), dayStart.getTime()));
      const end   = new Date(Math.min(ev.end.getTime(), dayEnd.getTime()));

      const startMin = start.getHours() * 60 + start.getMinutes();
      const endMin = end.getHours() * 60 + end.getMinutes();
      const dur = clamp(endMin - startMin, 10, minutesInDay);

      block.style.top = `${startMin * ppm}px`;
      block.style.height = `${dur * ppm}px`;

      block.innerHTML = `
          <div class="cal-event-head">
          <div class="t">${escapeHtml(ev.title)}</div>
          <div style="display:flex;flex-direction:column;align-items:flex-end;gap:4px;">
            <div class="time">${formatTime(ev.start)} – ${formatTime(ev.end)}</div>
            <div style="display:flex;align-items:center;gap:6px;">
              <div class="event-status-pill" data-ev-id="${escapeHtml(ev.id)}"></div>
            </div>
          </div>
         </div>
        <div class="d">${escapeHtml(ev.description || "")}</div>
      `;

      // set status on the indicator if available
      try {
        const statusVal = (window._lectureStatusMap && window._lectureStatusMap[ev.id] !== undefined) ? window._lectureStatusMap[ev.id] : (ev.status ?? ev.Status);
        const pill = block.querySelector('.event-status-pill');
        let label = '';
        if (pill) {
          pill.classList.remove('status-scheduled','status-inprogress','status-ended','status-canceled');
          if (String(statusVal).toLowerCase() === 'scheduled' || statusVal === 0) { pill.classList.add('status-scheduled'); label = 'Scheduled'; }
          else if (String(statusVal).toLowerCase() === 'inprogress' || statusVal === 1) { pill.classList.add('status-inprogress'); label = 'InProgress'; }
          else if (String(statusVal).toLowerCase() === 'ended' || statusVal === 2) { pill.classList.add('status-ended'); label = 'Ended'; }
          else if (String(statusVal).toLowerCase() === 'canceled' || statusVal === 3) { pill.classList.add('status-canceled'); label = 'Canceled'; }
          pill.textContent = label;
        }
      } catch (e) { /* ignore */ }

      // prevent closing when clicking an event and notify external handler
      block.addEventListener("click", (e) => {
        e.stopPropagation();
        try {
          // tag DOM node with event id for external positioning
          block.dataset.eventId = ev.id;
          if (typeof window.onCalendarEventClick === 'function') {
            window.onCalendarEventClick(ev);
            return;
          }
        } catch (err) { console.error(err); }
      });

      elEvents.appendChild(block);
    });

    // Optional: auto-scroll near "now" when viewing today
    const today = new Date();
    if (sameDay(state.selected, today)) {
      const minutesNow = today.getHours() * 60 + today.getMinutes();
      const targetY = minutesNow * ppm;
      // Scroll the timeline (the scroller is the parent of elEvents)
      const scroller = elEvents.parentElement; // .cal-timeline
      scroller.scrollTop = Math.max(0, targetY - 120);
    }
    renderNowLine();
  }

  // --- public API ---
  window.AttendanceCalendar = {
    open: () => openPopover(),
    close: () => closePopover(),
    toggle: () => togglePopover(),

    onOpen: (fn) => { state.onOpenCb = fn; },

    addTimespan: ({ title, description, start, minutes }) => {
      const s = (start instanceof Date) ? start : new Date(start);
      const e = new Date(s.getTime() + (minutes * 60 * 1000));
      return window.AttendanceCalendar.addEvent({ title, description, start: s, end: e });
    },

    addEvent: ({ id, title, description, start, end, status }) => {
      const s = (start instanceof Date) ? start : new Date(start);
      const e = (end instanceof Date) ? end : new Date(end);

      // Upsert by id to prevent duplicates when month loaders re-fetch
      const incomingId = (id !== undefined && id !== null) ? String(id) : null;
      if (incomingId) {
        const existingIndex = state.events.findIndex(x => String(x.id) === incomingId);
        if (existingIndex >= 0) {
          const existing = state.events[existingIndex];
          existing.title = title ?? existing.title ?? 'Untitled';
          existing.description = description ?? existing.description ?? '';
          existing.start = s;
          existing.end = e;
          if (status !== undefined) existing.status = status;

          // store status in global map for other scripts to read
          try { window._lectureStatusMap = window._lectureStatusMap || {}; if (incomingId && existing.status !== undefined) window._lectureStatusMap[incomingId] = existing.status; } catch (err) {}

          renderMonth();
          renderDay();
          return incomingId;
        }
      }

      const ev = {
        id: id ?? (crypto.randomUUID?.() ?? String(Date.now() + Math.random())),
        title: title ?? "Untitled",
        description: description ?? "",
        start: s,
        end: e
      };

      if (status !== undefined) ev.status = status;

      state.events.push(ev);
      // store status in global map for other scripts to read
      try { window._lectureStatusMap = window._lectureStatusMap || {}; if (ev.id && ev.status !== undefined) window._lectureStatusMap[ev.id] = ev.status; } catch(e){}
      renderMonth();
      renderDay();
      return ev.id;
    },

    // register a callback invoked when the month view changes for an unloaded month
    onViewChange: (fn) => { state.onViewChange = fn; },

    // mark month as loaded so the calendar won't request it again (and clear in-flight)
    markMonthLoaded: (monthKey) => { try { state.loadingMonths.delete(monthKey); state.loadedMonths.add(monthKey); } catch(e){} },
    // mark month as in-flight loading
    markMonthLoading: (monthKey) => { try { state.loadingMonths.add(monthKey); } catch(e){} },
    // return true if month is already fully loaded (not merely loading)
    isMonthLoaded: (monthKey) => { try { return state.loadedMonths.has(monthKey); } catch(e){ return false; } },

    clearEvents: () => {
      state.events = [];
      renderMonth();
      renderDay();
    }
  };

  // --- wire buttons ---
  btn.addEventListener("click", (e) => {
    e.stopPropagation();
    renderMonth();
    renderDay();
    togglePopover();
  });

  pop.querySelector("[data-cal-prev]")?.addEventListener("click", (e) => {
    e.stopPropagation();
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() - 1, 1);
    renderMonth();
  });

  pop.querySelector("[data-cal-next]")?.addEventListener("click", (e) => {
    e.stopPropagation();
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() + 1, 1);
    renderMonth();
  });

  pop.querySelector("[data-cal-today]")?.addEventListener("click", (e) => {
    e.stopPropagation();
    const t = new Date();
    t.setHours(0,0,0,0);
    state.view = new Date(t.getFullYear(), t.getMonth(), 1);
    state.selected = t;
    renderMonth();
    renderDay();
  });

  pop.querySelector("[data-cal-close]")?.addEventListener("click", (e) => {
    e.stopPropagation();
    closePopover();
  });

  // init
  const now = new Date();
  now.setHours(0,0,0,0);
  state.view = new Date(now.getFullYear(), now.getMonth(), 1);
  state.selected = now;
  renderMonth();
  renderDay();

  // ---- DEMO EVENTS (for visual testing) ----

setInterval(() => {
  renderNowLine();
}, 60 * 1000); // update every minute
})();
