/**
 * @fileoverview Profile calendar component.
 * Displays a professor's lectures in a calendar view with day timeline.
 */

'use strict';

(function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Hours in a day */
  const HOURS_IN_DAY = 24;

  /** Minutes in a day */
  const MINUTES_IN_DAY = HOURS_IN_DAY * 60;

  /** Milliseconds per minute */
  const MS_PER_MINUTE = 60 * 1000;

  /** Minimum event duration in minutes for display */
  const MIN_EVENT_DURATION = 15;

  /** Default duration in minutes when not specified */
  const DEFAULT_DURATION_MINUTES = 60;

  /** Now line update interval in milliseconds */
  const NOW_LINE_UPDATE_INTERVAL_MS = 60000;

  /** Default pixels per minute for timeline */
  const DEFAULT_PPM = 1;

  /** Days in a week */
  const DAYS_IN_WEEK = 7;

  /** Scroll offset to position current time nicely in view */
  const SCROLL_OFFSET = 120;

  /** Lecture status mapping */
  const StatusConfig = Object.freeze({
    SCHEDULED: { value: 0, label: 'Scheduled', class: 'status-scheduled' },
    IN_PROGRESS: { value: 1, label: 'InProgress', class: 'status-inprogress' },
    ENDED: { value: 2, label: 'Ended', class: 'status-ended' },
    CANCELED: { value: 3, label: 'Canceled', class: 'status-canceled' }
  });

  // ============================================================================
  // DOM Element Setup
  // ============================================================================

  const calendarRoot = document.getElementById('profileCalendar');

  // Early exit if calendar element doesn't exist or user is not a professor
  if (!calendarRoot || !globalThis.profileData?.isProfessor) {
    return;
  }

  const professorId = globalThis.profileData.userId;
  if (!professorId) {
    return;
  }

  const elements = {
    month: calendarRoot.querySelector('[data-cal-month]'),
    grid: calendarRoot.querySelector('[data-cal-grid]'),
    dayLabel: calendarRoot.querySelector('[data-cal-daylabel]'),
    hours: calendarRoot.querySelector('[data-cal-hours]'),
    events: calendarRoot.querySelector('[data-cal-events]'),
    prevBtn: calendarRoot.querySelector('[data-cal-prev]'),
    nextBtn: calendarRoot.querySelector('[data-cal-next]'),
    todayBtn: calendarRoot.querySelector('[data-cal-today]')
  };

  // Validate required elements
  if (!elements.month || !elements.grid || !elements.dayLabel || !elements.hours || !elements.events) {
    console.warn('[ProfileCalendar] Missing required elements');
    return;
  }

  // ============================================================================
  // State
  // ============================================================================

  const state = {
    view: new Date(),
    selected: new Date(),
    events: [],
    loadedMonths: new Set()
  };

  /** Reference to the now line element */
  let nowLineElement = null;

  // ============================================================================
  // Utility Functions
  // ============================================================================

  /**
   * Pads a number to 2 digits with leading zero.
   * @param {number} num - The number to pad.
   * @returns {string} The padded string.
   */
  function padTwo(num) {
    return String(num).padStart(2, '0');
  }

  /**
   * Checks if two dates are the same day.
   * @param {Date} dateA - First date.
   * @param {Date} dateB - Second date.
   * @returns {boolean} True if same day.
   */
  function isSameDay(dateA, dateB) {
    return dateA.getFullYear() === dateB.getFullYear() &&
           dateA.getMonth() === dateB.getMonth() &&
           dateA.getDate() === dateB.getDate();
  }

  /**
   * Checks if an event overlaps with a specific day.
   * @param {Object} event - The event with start and end dates.
   * @param {Date} day - The day to check.
   * @returns {boolean} True if event overlaps the day.
   */
  function eventOverlapsDay(event, day) {
    const start = new Date(event.start);
    const end = new Date(event.end);
    const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0, 0, 0, 0);
    const dayEnd = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999);
    return start <= dayEnd && end >= dayStart;
  }

  /**
   * Clamps a value between min and max.
   * @param {number} value - The value to clamp.
   * @param {number} min - Minimum value.
   * @param {number} max - Maximum value.
   * @returns {number} The clamped value.
   */
  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  /**
   * Escapes HTML special characters to prevent XSS.
   * @param {string} str - The string to escape.
   * @returns {string} The escaped string.
   */
  function escapeHtml(str) {
    if (!str) {
      return '';
    }
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  /**
   * Adds days to a date.
   * @param {Date} date - The source date.
   * @param {number} days - Number of days to add.
   * @returns {Date} The new date.
   */
  function addDays(date, days) {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  // ============================================================================
  // Formatting Functions
  // ============================================================================

  /**
   * Formats a date as month and year.
   * @param {Date} date - The date to format.
   * @returns {string} The formatted string.
   */
  function formatMonth(date) {
    return date.toLocaleString(undefined, { month: 'long', year: 'numeric' });
  }

  /**
   * Formats a date as a full day label.
   * @param {Date} date - The date to format.
   * @returns {string} The formatted string.
   */
  function formatDayLabel(date) {
    return date.toLocaleString(undefined, {
      weekday: 'long',
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  /**
   * Formats a date as time (HH:MM).
   * @param {Date} date - The date to format.
   * @returns {string} The formatted time string.
   */
  function formatTime(date) {
    return `${padTwo(date.getHours())}:${padTwo(date.getMinutes())}`;
  }

  // ============================================================================
  // Timeline Functions
  // ============================================================================

  /**
   * Gets the pixels per minute value from CSS custom property.
   * @returns {number} The PPM value.
   */
  function getPixelsPerMinute() {
    const ppmRaw = getComputedStyle(elements.events).getPropertyValue('--ppm').trim();
    const ppm = Number.parseFloat(ppmRaw);
    return Number.isFinite(ppm) && ppm > 0 ? ppm : DEFAULT_PPM;
  }

  // ============================================================================
  // Duration Parsing
  // ============================================================================

  /**
   * Parses ISO 8601 duration format.
   * @param {string} input - The duration string in uppercase.
   * @returns {number} The duration in minutes.
   */
  function parseIsoDuration(input) {
    const hours = Number((input.match(/(\d+)H/) || [0, 0])[1] || 0);
    const minutes = Number((input.match(/(\d+)M/) || [0, 0])[1] || 0);
    const seconds = Number((input.match(/(\d+)S/) || [0, 0])[1] || 0);
    return hours * 60 + minutes + Math.round(seconds / 60);
  }

  /**
   * Parses a duration input into minutes.
   * @param {number|string|null} input - The duration input.
   * @returns {number} The duration in minutes.
   */
  function parseDurationToMinutes(input) {
    if (input == null) {
      return DEFAULT_DURATION_MINUTES;
    }

    if (typeof input === 'number') {
      return Math.max(0, Math.floor(input));
    }

    const str = String(input).trim();

    // ISO 8601 duration (PT1H30M)
    if (/^P/i.test(str)) {
      return parseIsoDuration(str.toUpperCase());
    }

    // HH:MM:SS format
    const hhmmssMatch = str.match(/^(\d{1,2}):(\d{2}):(\d{2})$/);
    if (hhmmssMatch) {
      return Number(hhmmssMatch[1]) * 60 + Number(hhmmssMatch[2]) + Math.round(Number(hhmmssMatch[3]) / 60);
    }

    // HH:MM format
    const hhmmMatch = str.match(/^(\d{1,2}):(\d{2})$/);
    if (hhmmMatch) {
      return Number(hhmmMatch[1]) * 60 + Number(hhmmMatch[2]);
    }

    return DEFAULT_DURATION_MINUTES;
  }

  // ============================================================================
  // Status Functions
  // ============================================================================

  /**
   * Gets the status configuration for a status value.
   * @param {number|string} statusValue - The status value.
   * @returns {Object} Object with label and class properties.
   */
  function getStatusConfig(statusValue) {
    const statusLower = String(statusValue || '').toLowerCase();

    if (statusLower === 'scheduled' || statusValue === StatusConfig.SCHEDULED.value) {
      return StatusConfig.SCHEDULED;
    }

    if (statusLower === 'inprogress' || statusLower === 'active' || statusValue === StatusConfig.IN_PROGRESS.value) {
      return StatusConfig.IN_PROGRESS;
    }

    if (statusLower === 'ended' || statusValue === StatusConfig.ENDED.value) {
      return StatusConfig.ENDED;
    }

    if (statusLower === 'canceled' || statusValue === StatusConfig.CANCELED.value) {
      return StatusConfig.CANCELED;
    }

    return { label: '', class: '' };
  }

  // ============================================================================
  // Event Management
  // ============================================================================

  /**
   * Adds an event to the calendar state.
   * @param {Object} item - The lecture item from the API.
   */
  function addEvent(item) {
    const id = item.id || item.Id;
    const name = item.name || item.Name || 'Untitled';
    const startTime = item.startTime || item.StartTime;
    const description = item.description || item.Description || '';
    const duration = item.duration || item.Duration;
    const status = item.status || item.Status || '';

    if (!startTime) {
      return;
    }

    // Check for duplicates
    const existingIndex = state.events.findIndex(function (ev) {
      return ev.id === id;
    });

    if (existingIndex !== -1) {
      return;
    }

    const startDate = new Date(startTime);
    if (isNaN(startDate.getTime())) {
      return;
    }

    const minutes = parseDurationToMinutes(duration);
    const endDate = new Date(startDate.getTime() + minutes * MS_PER_MINUTE);

    state.events.push({
      id: id,
      title: name,
      description: description,
      status: status,
      start: startDate,
      end: endDate
    });
  }

  // ============================================================================
  // Rendering Functions
  // ============================================================================

  /**
   * Creates a calendar cell element.
   * @param {Date} cellDate - The date for this cell.
   * @param {boolean} inMonth - Whether the cell is in the current month.
   * @returns {HTMLElement} The cell element.
   */
  function createCalendarCell(cellDate, inMonth) {
    const cell = document.createElement('div');
    cell.className = 'cal-cell' + (inMonth ? '' : ' muted');

    if (isSameDay(cellDate, state.selected)) {
      cell.classList.add('selected');
    }

    // Day number
    const dayNum = document.createElement('div');
    dayNum.className = 'cal-daynum';
    dayNum.textContent = cellDate.getDate();
    cell.appendChild(dayNum);

    // Event dots
    const dayEvents = state.events.filter(function (ev) {
      return eventOverlapsDay(ev, cellDate);
    });

    if (dayEvents.length > 0) {
      const dots = document.createElement('div');
      dots.className = 'cal-dots';
      const dot = document.createElement('span');
      dot.className = 'cal-dot';
      dots.appendChild(dot);
      cell.appendChild(dots);
    }

    // Click handler
    cell.addEventListener('click', function () {
      state.selected = cellDate;
      renderMonth();
      renderDay();
    });

    return cell;
  }

  /**
   * Renders the month grid.
   */
  function renderMonth() {
    elements.month.textContent = formatMonth(state.view);
    elements.grid.innerHTML = '';

    const year = state.view.getFullYear();
    const month = state.view.getMonth();
    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    // Calculate start day (Monday = 1, Sunday = 7)
    let startDay = firstDayOfMonth.getDay();
    if (startDay === 0) {
      startDay = 7;
    }

    const prevMonthDays = startDay - 1;
    const daysInMonth = lastDayOfMonth.getDate();
    const totalCells = Math.ceil((prevMonthDays + daysInMonth) / DAYS_IN_WEEK) * DAYS_IN_WEEK;

    // Create cells
    for (let i = 0; i < totalCells; i++) {
      const dayOffset = i - prevMonthDays + 1;
      const cellDate = new Date(year, month, dayOffset);
      const inMonth = dayOffset >= 1 && dayOffset <= daysInMonth;

      const cell = createCalendarCell(cellDate, inMonth);
      elements.grid.appendChild(cell);
    }

    // Load events for visible months
    loadEventsForMonth(year, month);

    if (prevMonthDays > 0) {
      loadEventsForMonth(year, month - 1);
    }

    if (totalCells - prevMonthDays - daysInMonth > 0) {
      loadEventsForMonth(year, month + 1);
    }
  }

  /**
   * Creates an event element for the day timeline.
   * @param {Object} event - The event object.
   * @param {Date} dayStart - Start of the day.
   * @param {Date} dayEnd - End of the day.
   * @param {number} ppm - Pixels per minute.
   * @returns {HTMLElement} The event element.
   */
  function createEventElement(event, dayStart, dayEnd, ppm) {
    // Clamp event to day boundaries
    const start = new Date(Math.max(event.start.getTime(), dayStart.getTime()));
    const end = new Date(Math.min(event.end.getTime(), dayEnd.getTime()));

    const startMinutes = start.getHours() * 60 + start.getMinutes();
    const endMinutes = end.getHours() * 60 + end.getMinutes();
    const duration = clamp(endMinutes - startMinutes, MIN_EVENT_DURATION, MINUTES_IN_DAY);

    const eventEl = document.createElement('div');
    eventEl.className = 'cal-ev';
    eventEl.style.top = `${startMinutes * ppm}px`;
    eventEl.style.height = `${duration * ppm}px`;

    // Get status styling
    const statusConfig = getStatusConfig(event.status);

    // Build event HTML
    const descriptionHtml = event.description
      ? `<div class="d">${escapeHtml(event.description)}</div>`
      : '';

    eventEl.innerHTML = `
      <div class="cal-ev-head">
        <div class="t">${escapeHtml(event.title || 'Untitled')}</div>
        <div style="display:flex;flex-direction:column;align-items:flex-end;gap:4px;">
          <div class="time">${formatTime(event.start)} – ${formatTime(event.end)}</div>
          <div style="display:flex;align-items:center;gap:6px;">
            <div class="event-status-pill ${statusConfig.class}">${statusConfig.label}</div>
          </div>
        </div>
      </div>
      ${descriptionHtml}
    `;

    return eventEl;
  }

  /**
   * Renders the day timeline.
   */
  function renderDay() {
    elements.dayLabel.textContent = formatDayLabel(state.selected);
    elements.hours.innerHTML = '';
    elements.events.innerHTML = '';

    const ppm = getPixelsPerMinute();

    // Render hour labels
    for (let hour = 0; hour < HOURS_IN_DAY; hour++) {
      const hourEl = document.createElement('div');
      hourEl.className = 'cal-hour';
      hourEl.textContent = `${padTwo(hour)}:00`;
      elements.hours.appendChild(hourEl);
    }

    // Get events for selected day
    const dayEvents = state.events
      .filter(function (ev) {
        return eventOverlapsDay(ev, state.selected);
      })
      .sort(function (a, b) {
        return a.start - b.start;
      });

    // Day boundaries
    const dayStart = new Date(
      state.selected.getFullYear(),
      state.selected.getMonth(),
      state.selected.getDate(),
      0, 0, 0, 0
    );
    const dayEnd = new Date(
      state.selected.getFullYear(),
      state.selected.getMonth(),
      state.selected.getDate(),
      23, 59, 59, 999
    );

    // Render events
    for (const event of dayEvents) {
      const eventEl = createEventElement(event, dayStart, dayEnd, ppm);
      elements.events.appendChild(eventEl);
    }

    // Auto-scroll to current time if viewing today
    const today = new Date();
    if (isSameDay(state.selected, today)) {
      const minutesNow = today.getHours() * 60 + today.getMinutes();
      const targetY = minutesNow * ppm;
      const scroller = elements.events.parentElement;
      // Use setTimeout to ensure layout is complete before scrolling
      setTimeout(function() {
        scroller.scrollTop = Math.max(0, targetY - SCROLL_OFFSET);
      }, 0);
    }

    renderNowLine();
  }

  /**
   * Renders or updates the "now" line on the timeline.
   */
  function renderNowLine() {
    const today = new Date();

    if (!isSameDay(state.selected, today)) {
      if (nowLineElement) {
        nowLineElement.remove();
      }
      nowLineElement = null;
      return;
    }

    const ppm = getPixelsPerMinute();
    const minutesNow = today.getHours() * 60 + today.getMinutes();
    const yPosition = minutesNow * ppm;

    if (!nowLineElement) {
      nowLineElement = document.createElement('div');
      nowLineElement.className = 'cal-now';
    }

    nowLineElement.style.top = `${yPosition}px`;
    elements.events.appendChild(nowLineElement);
  }

  // ============================================================================
  // API Functions
  // ============================================================================

  /**
   * Calculates months ago from a given year and month.
   * @param {number} year - The target year.
   * @param {number} month - The target month (0-based).
   * @returns {number} Number of months ago (can be negative for future).
   */
  function calculateMonthsAgo(year, month) {
    const now = new Date();
    return (now.getFullYear() * 12 + now.getMonth()) - (year * 12 + month);
  }

  /**
   * Creates a month key for tracking loaded months.
   * @param {number} year - The year.
   * @param {number} month - The month (0-based).
   * @returns {string} The month key.
   */
  function createMonthKey(year, month) {
    return `${year}-${month}`;
  }

  /**
   * Loads events for a specific month from the API.
   * @param {number} year - The year.
   * @param {number} month - The month (0-based).
   */
  async function loadEventsForMonth(year, month) {
    const key = createMonthKey(year, month);

    if (state.loadedMonths.has(key)) {
      return;
    }

    state.loadedMonths.add(key);

    const monthsAgo = calculateMonthsAgo(year, month);

    try {
      const params = new URLSearchParams({
        page: '0',
        pageSize: '200'
      });

      // Only pass fromMonthsAgo if we're looking at a past month
      if (monthsAgo >= 0) {
        params.set('fromMonthsAgo', String(monthsAgo));
      }

      const response = await fetch(`/api/lectures/${professorId}?${params}`, {
        method: 'GET',
        credentials: 'include',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return;
      }

      const data = await response.json();

      if (!Array.isArray(data)) {
        return;
      }

      for (const item of data) {
        addEvent(item);
      }

      renderMonth();
      renderDay();
    } catch (error) {
      console.error('[ProfileCalendar] Failed to load lectures:', error);
    }
  }

  // ============================================================================
  // Navigation Handlers
  // ============================================================================

  /**
   * Navigates to the previous month.
   */
  function navigateToPreviousMonth() {
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() - 1, 1);
    renderMonth();
  }

  /**
   * Navigates to the next month.
   */
  function navigateToNextMonth() {
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() + 1, 1);
    renderMonth();
  }

  /**
   * Navigates to today.
   */
  function navigateToToday() {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    state.view = new Date(today.getFullYear(), today.getMonth(), 1);
    state.selected = today;
    renderMonth();
    renderDay();
  }

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes event listeners.
   */
  function initEventListeners() {
    if (elements.prevBtn) {
      elements.prevBtn.addEventListener('click', navigateToPreviousMonth);
    }

    if (elements.nextBtn) {
      elements.nextBtn.addEventListener('click', navigateToNextMonth);
    }

    if (elements.todayBtn) {
      elements.todayBtn.addEventListener('click', navigateToToday);
    }
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  /**
   * Initializes the calendar.
   */
  function init() {
    // Set initial state
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    state.view = new Date(now.getFullYear(), now.getMonth(), 1);
    state.selected = now;

    // Set up event listeners
    initEventListeners();

    // Initial render
    renderMonth();
    renderDay();

    // Update now line every minute
    setInterval(renderNowLine, NOW_LINE_UPDATE_INTERVAL_MS);
  }

  init();
})();
