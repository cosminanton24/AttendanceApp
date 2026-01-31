/**
 * @fileoverview Home page functionality.
 * Contains the Attendance Calendar popover and User Search components.
 */

'use strict';

// =============================================================================
// ATTENDANCE CALENDAR COMPONENT
// =============================================================================

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

  /** Days to display in month grid */
  const MONTH_GRID_CELLS = 42;

  /** Minimum event duration in minutes for display */
  const MIN_EVENT_DURATION = 10;

  /** Default pixels per minute for timeline */
  const DEFAULT_PPM = 1.25;

  /** Scroll offset from current time (pixels) */
  const SCROLL_OFFSET = 120;

  /** Now line update interval in milliseconds */
  const NOW_LINE_UPDATE_INTERVAL_MS = 60 * 1000;

  // ============================================================================
  // DOM Elements
  // ============================================================================

  const calendarBtn = document.getElementById('calendarBtn');
  const popover = document.getElementById('calendarPopover');

  if (!calendarBtn || !popover) {
    console.warn('[AttendanceCalendar] Missing #calendarBtn or #calendarPopover');
    return;
  }

  const elements = {
    month: popover.querySelector('[data-cal-month]'),
    grid: popover.querySelector('[data-cal-grid]'),
    dayLabel: popover.querySelector('[data-cal-daylabel]'),
    hours: popover.querySelector('[data-cal-hours]'),
    events: popover.querySelector('[data-cal-events]'),
    prevBtn: popover.querySelector('[data-cal-prev]'),
    nextBtn: popover.querySelector('[data-cal-next]'),
    todayBtn: popover.querySelector('[data-cal-today]'),
    closeBtn: popover.querySelector('[data-cal-close]'),
    dayHead: popover.querySelector('.cal-day-head')
  };

  // Prevent clicks inside popover from closing it
  popover.addEventListener('click', function (e) {
    e.stopPropagation();
  });

  // Validate required elements
  if (!elements.month || !elements.grid || !elements.dayLabel || !elements.hours || !elements.events) {
    console.warn('[AttendanceCalendar] Missing one or more [data-cal-*] elements');
    return;
  }

  // ============================================================================
  // State
  // ============================================================================

  const state = {
    view: new Date(),
    selected: new Date(),
    events: [],
    onOpenCb: null,
    loadedMonths: new Set(),
    loadingMonths: new Set(),
    onViewChange: null
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

  /**
   * Escapes HTML special characters to prevent XSS.
   * @param {string} str - The string to escape.
   * @returns {string} The escaped string.
   */
  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  /**
   * Generates a unique ID.
   * @returns {string} A unique identifier.
   */
  function generateId() {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    return String(Date.now() + Math.random());
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

  /**
   * Scrolls the timeline to show the current time.
   */
  function scrollToNowIfToday() {
    const today = new Date();

    if (!isSameDay(state.selected, today)) {
      return;
    }

    const ppm = getPixelsPerMinute();
    const minutesNow = today.getHours() * 60 + today.getMinutes();
    const targetY = minutesNow * ppm;

    const scroller = elements.events.parentElement;
    scroller.scrollTop = Math.max(0, targetY - SCROLL_OFFSET);
  }

  // ============================================================================
  // Popover Functions
  // ============================================================================

  /**
   * Opens the calendar popover.
   */
  function openPopover() {
    popover.classList.add('open');
    popover.setAttribute('aria-hidden', 'false');

    if (typeof state.onOpenCb === 'function') {
      state.onOpenCb();
    }

    scrollToNowIfToday();
  }

  /**
   * Closes the calendar popover.
   */
  function closePopover() {
    popover.classList.remove('open');
    popover.setAttribute('aria-hidden', 'true');
  }

  /**
   * Toggles the calendar popover.
   */
  function togglePopover() {
    if (popover.classList.contains('open')) {
      closePopover();
    } else {
      openPopover();
    }
  }

  // ============================================================================
  // Event Overlap Detection
  // ============================================================================

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

  // ============================================================================
  // Status Functions
  // ============================================================================

  /**
   * Gets the status class and label for a status value.
   * @param {number|string} statusValue - The status value.
   * @returns {Object} Object with cssClass and label properties.
   */
  function getStatusInfo(statusValue) {
    const statusLower = String(statusValue || '').toLowerCase();

    if (statusLower === 'scheduled' || statusValue === 0) {
      return { cssClass: 'status-scheduled', label: 'Scheduled' };
    }

    if (statusLower === 'inprogress' || statusValue === 1) {
      return { cssClass: 'status-inprogress', label: 'InProgress' };
    }

    if (statusLower === 'ended' || statusValue === 2) {
      return { cssClass: 'status-ended', label: 'Ended' };
    }

    if (statusLower === 'canceled' || statusValue === 3) {
      return { cssClass: 'status-canceled', label: 'Canceled' };
    }

    return { cssClass: '', label: '' };
  }

  /**
   * Updates the status pill element for an event.
   * @param {HTMLElement} pillElement - The pill element.
   * @param {number|string} statusValue - The status value.
   */
  function updateStatusPill(pillElement, statusValue) {
    if (!pillElement) {
      return;
    }

    pillElement.classList.remove(
      'status-scheduled',
      'status-inprogress',
      'status-ended',
      'status-canceled'
    );

    const statusInfo = getStatusInfo(statusValue);

    if (statusInfo.cssClass) {
      pillElement.classList.add(statusInfo.cssClass);
    }

    pillElement.textContent = statusInfo.label;
  }

  // ============================================================================
  // Month Rendering
  // ============================================================================

  /**
   * Notifies view change listeners for month loading.
   */
  function notifyViewChange() {
    if (typeof state.onViewChange !== 'function') {
      return;
    }

    const currentMonth = new Date(state.view.getFullYear(), state.view.getMonth(), 1);
    const previousMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1);
    const nextMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1);

    const monthsToCheck = [previousMonth, currentMonth, nextMonth];

    for (const monthDate of monthsToCheck) {
      const monthKey = `${monthDate.getFullYear()}-${monthDate.getMonth() + 1}`;

      if (!state.loadedMonths.has(monthKey) && !state.loadingMonths.has(monthKey)) {
        state.loadingMonths.add(monthKey);

        try {
          state.onViewChange(monthKey, new Date(monthDate));
        } catch (error) {
          console.error('[AttendanceCalendar] View change error:', error);
        }
      }
    }
  }

  /**
   * Creates a calendar cell element.
   * @param {Date} cellDate - The date for this cell.
   * @param {number} currentMonth - The current month being displayed.
   * @returns {HTMLElement} The cell element.
   */
  function createMonthCell(cellDate, currentMonth) {
    const cell = document.createElement('div');
    cell.className = 'cal-cell';

    if (cellDate.getMonth() !== currentMonth) {
      cell.classList.add('muted');
    }

    if (isSameDay(cellDate, state.selected)) {
      cell.classList.add('selected');
    }

    // Day number
    const dayNum = document.createElement('div');
    dayNum.className = 'cal-daynum';
    dayNum.textContent = cellDate.getDate();

    // Event dot indicator
    const hasEvents = state.events.some(function (ev) {
      return eventOverlapsDay(ev, cellDate);
    });

    if (hasEvents) {
      const dot = document.createElement('span');
      dot.className = 'cal-dot';
      cell.appendChild(dot);
    }

    cell.appendChild(dayNum);

    // Click handler
    const selectedDate = new Date(cellDate.getFullYear(), cellDate.getMonth(), cellDate.getDate());
    cell.addEventListener('click', function (e) {
      e.stopPropagation();
      state.selected = selectedDate;
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
    // Convert to Monday-based week (Mon=0..Sun=6)
    const firstDayOfWeek = (firstDayOfMonth.getDay() + 6) % 7;
    const startDate = new Date(year, month, 1 - firstDayOfWeek);

    for (let i = 0; i < MONTH_GRID_CELLS; i++) {
      const cellDate = new Date(startDate);
      cellDate.setDate(startDate.getDate() + i);

      const cell = createMonthCell(cellDate, month);
      elements.grid.appendChild(cell);
    }

    // Notify listeners for lazy loading
    try {
      notifyViewChange();
    } catch {
      // Ignore view change notification errors
    }
  }

  // ============================================================================
  // Day Rendering
  // ============================================================================

  /**
   * Creates an event block element.
   * @param {Object} event - The event object.
   * @param {number} ppm - Pixels per minute.
   * @returns {HTMLElement} The event block element.
   */
  function createEventBlock(event, ppm) {
    const block = document.createElement('div');
    block.className = 'cal-event';

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

    // Clamp event to day boundaries
    const start = new Date(Math.max(event.start.getTime(), dayStart.getTime()));
    const end = new Date(Math.min(event.end.getTime(), dayEnd.getTime()));

    const startMinutes = start.getHours() * 60 + start.getMinutes();
    const endMinutes = end.getHours() * 60 + end.getMinutes();
    const duration = clamp(endMinutes - startMinutes, MIN_EVENT_DURATION, MINUTES_IN_DAY);

    block.style.top = `${startMinutes * ppm}px`;
    block.style.height = `${duration * ppm}px`;

    block.innerHTML = `
      <div class="cal-event-head">
        <div class="t">${escapeHtml(event.title)}</div>
        <div style="display:flex;flex-direction:column;align-items:flex-end;gap:4px;">
          <div class="time">${formatTime(event.start)} – ${formatTime(event.end)}</div>
          <div style="display:flex;align-items:center;gap:6px;">
            <div class="event-status-pill" data-ev-id="${escapeHtml(event.id)}"></div>
          </div>
        </div>
      </div>
      <div class="d">${escapeHtml(event.description || '')}</div>
    `;

    // Update status pill
    try {
      const statusMap = globalThis._lectureStatusMap || {};
      const statusValue = statusMap[event.id] !== undefined
        ? statusMap[event.id]
        : (event.status ?? event.Status);

      const pill = block.querySelector('.event-status-pill');
      updateStatusPill(pill, statusValue);
    } catch {
      // Ignore status update errors
    }

    // Click handler
    block.addEventListener('click', function (e) {
      e.stopPropagation();
      block.dataset.eventId = event.id;

      try {
        if (typeof globalThis.onCalendarEventClick === 'function') {
          globalThis.onCalendarEventClick(event);
        }
      } catch (error) {
        console.error('[AttendanceCalendar] Event click error:', error);
      }
    });

    return block;
  }

  /**
   * Renders the now line indicator.
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

    if (!elements.events) {
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
      const label = document.createElement('div');
      label.className = 'cal-hour';
      label.textContent = `${padTwo(hour)}:00`;
      label.style.top = `${hour * 60 * ppm}px`;

      if (hour === 0) {
        label.style.transform = 'translateY(6px)';
      }

      if (hour % 2 === 1) {
        label.classList.add('odd');
      }

      elements.hours.appendChild(label);
    }

    // Get and sort events for selected day
    const dayEvents = state.events
      .filter(function (ev) {
        return eventOverlapsDay(ev, state.selected);
      })
      .sort(function (a, b) {
        return a.start - b.start;
      });

    // Render event blocks
    for (const event of dayEvents) {
      const block = createEventBlock(event, ppm);
      elements.events.appendChild(block);
    }

    // Auto-scroll to current time if viewing today
    const today = new Date();
    if (isSameDay(state.selected, today)) {
      const minutesNow = today.getHours() * 60 + today.getMinutes();
      const targetY = minutesNow * ppm;
      const scroller = elements.events.parentElement;
      scroller.scrollTop = Math.max(0, targetY - SCROLL_OFFSET);
    }

    renderNowLine();
  }

  // ============================================================================
  // Mobile Day Navigation
  // ============================================================================

  /**
   * Initializes mobile day navigation buttons.
   */
  function initMobileDayNavigation() {
    if (!elements.dayHead || elements.dayHead.querySelector('.cal-day-nav')) {
      return;
    }

    const nav = document.createElement('div');
    nav.className = 'cal-day-nav';
    nav.innerHTML = `
      <button type="button" class="cal-day-btn" data-day-prev aria-label="Previous day">‹</button>
      <button type="button" class="cal-day-btn" data-day-next aria-label="Next day">›</button>
    `;
    elements.dayHead.appendChild(nav);

    nav.querySelector('[data-day-prev]').addEventListener('click', function (e) {
      e.stopPropagation();
      state.selected = addDays(state.selected, -1);
      state.view = new Date(state.selected.getFullYear(), state.selected.getMonth(), 1);
      renderMonth();
      renderDay();
    });

    nav.querySelector('[data-day-next]').addEventListener('click', function (e) {
      e.stopPropagation();
      state.selected = addDays(state.selected, 1);
      state.view = new Date(state.selected.getFullYear(), state.selected.getMonth(), 1);
      renderMonth();
      renderDay();
    });
  }

  // ============================================================================
  // Public API
  // ============================================================================

  /**
   * Ensures global status map exists.
   */
  function ensureStatusMap() {
    globalThis._lectureStatusMap = globalThis._lectureStatusMap || {};
  }

  /**
   * Updates an existing event in the state.
   * @param {number} index - The event index.
   * @param {Object} params - The event parameters.
   * @returns {string} The event ID.
   */
  function updateExistingEvent(index, params) {
    const existing = state.events[index];
    const { title, description, start, end, status } = params;

    existing.title = title ?? existing.title ?? 'Untitled';
    existing.description = description ?? existing.description ?? '';
    existing.start = start;
    existing.end = end;

    if (status !== undefined) {
      existing.status = status;
    }

    // Update global status map
    ensureStatusMap();
    if (existing.id && existing.status !== undefined) {
      globalThis._lectureStatusMap[existing.id] = existing.status;
    }

    renderMonth();
    renderDay();

    return existing.id;
  }

  /**
   * Creates a new event in the state.
   * @param {Object} params - The event parameters.
   * @returns {string} The event ID.
   */
  function createNewEvent(params) {
    const { id, title, description, start, end, status } = params;

    const event = {
      id: id ?? generateId(),
      title: title ?? 'Untitled',
      description: description ?? '',
      start: start,
      end: end
    };

    if (status !== undefined) {
      event.status = status;
    }

    state.events.push(event);

    // Update global status map
    ensureStatusMap();
    if (event.id && event.status !== undefined) {
      globalThis._lectureStatusMap[event.id] = event.status;
    }

    renderMonth();
    renderDay();

    return event.id;
  }

  // Expose public API
  globalThis.AttendanceCalendar = {
    /**
     * Opens the calendar popover.
     */
    open: function () {
      openPopover();
    },

    /**
     * Closes the calendar popover.
     */
    close: function () {
      closePopover();
    },

    /**
     * Toggles the calendar popover.
     */
    toggle: function () {
      togglePopover();
    },

    /**
     * Registers a callback for when the calendar opens.
     * @param {Function} fn - The callback function.
     */
    onOpen: function (fn) {
      state.onOpenCb = fn;
    },

    /**
     * Adds an event using a timespan (start + duration).
     * @param {Object} params - Event parameters.
     * @returns {string} The event ID.
     */
    addTimespan: function (params) {
      const { title, description, start, minutes } = params;
      const startDate = (start instanceof Date) ? start : new Date(start);
      const endDate = new Date(startDate.getTime() + (minutes * MS_PER_MINUTE));
      return globalThis.AttendanceCalendar.addEvent({
        title: title,
        description: description,
        start: startDate,
        end: endDate
      });
    },

    /**
     * Adds or updates an event.
     * @param {Object} params - Event parameters.
     * @returns {string} The event ID.
     */
    addEvent: function (params) {
      const { id, title, description, start, end, status } = params;
      const startDate = (start instanceof Date) ? start : new Date(start);
      const endDate = (end instanceof Date) ? end : new Date(end);

      // Check for existing event with same ID
      const incomingId = (id !== undefined && id !== null) ? String(id) : null;

      if (incomingId) {
        const existingIndex = state.events.findIndex(function (x) {
          return String(x.id) === incomingId;
        });

        if (existingIndex >= 0) {
          return updateExistingEvent(existingIndex, {
            title: title,
            description: description,
            start: startDate,
            end: endDate,
            status: status
          });
        }
      }

      return createNewEvent({
        id: id,
        title: title,
        description: description,
        start: startDate,
        end: endDate,
        status: status
      });
    },

    /**
     * Registers a callback for month view changes (for lazy loading).
     * @param {Function} fn - The callback function.
     */
    onViewChange: function (fn) {
      state.onViewChange = fn;
    },

    /**
     * Marks a month as fully loaded.
     * @param {string} monthKey - The month key (e.g., "2026-2").
     */
    markMonthLoaded: function (monthKey) {
      try {
        state.loadingMonths.delete(monthKey);
        state.loadedMonths.add(monthKey);
      } catch {
        // Ignore errors
      }
    },

    /**
     * Marks a month as currently loading.
     * @param {string} monthKey - The month key.
     */
    markMonthLoading: function (monthKey) {
      try {
        state.loadingMonths.add(monthKey);
      } catch {
        // Ignore errors
      }
    },

    /**
     * Checks if a month is fully loaded.
     * @param {string} monthKey - The month key.
     * @returns {boolean} True if loaded.
     */
    isMonthLoaded: function (monthKey) {
      try {
        return state.loadedMonths.has(monthKey);
      } catch {
        return false;
      }
    },

    /**
     * Clears all events from the calendar.
     */
    clearEvents: function () {
      state.events = [];
      renderMonth();
      renderDay();
    }
  };

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes calendar event listeners.
   */
  function initEventListeners() {
    // Calendar toggle button
    calendarBtn.addEventListener('click', function (e) {
      e.stopPropagation();
      renderMonth();
      renderDay();
      togglePopover();
    });

    // Previous month
    if (elements.prevBtn) {
      elements.prevBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        state.view = new Date(state.view.getFullYear(), state.view.getMonth() - 1, 1);
        renderMonth();
      });
    }

    // Next month
    if (elements.nextBtn) {
      elements.nextBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        state.view = new Date(state.view.getFullYear(), state.view.getMonth() + 1, 1);
        renderMonth();
      });
    }

    // Today button
    if (elements.todayBtn) {
      elements.todayBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        state.view = new Date(today.getFullYear(), today.getMonth(), 1);
        state.selected = today;
        renderMonth();
        renderDay();
      });
    }

    // Close button
    if (elements.closeBtn) {
      elements.closeBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        closePopover();
      });
    }

    // Close on outside click
    document.addEventListener('click', function (e) {
      if (!popover.classList.contains('open')) {
        return;
      }

      if (popover.contains(e.target) || calendarBtn.contains(e.target)) {
        return;
      }

      closePopover();
    });
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  /**
   * Initializes the calendar component.
   */
  function init() {
    // Set initial state
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    state.view = new Date(now.getFullYear(), now.getMonth(), 1);
    state.selected = now;

    // Initialize components
    initMobileDayNavigation();
    initEventListeners();

    // Initial render
    renderMonth();
    renderDay();

    // Update now line periodically
    setInterval(renderNowLine, NOW_LINE_UPDATE_INTERVAL_MS);
  }

  init();
})();


// =============================================================================
// USER SEARCH COMPONENT
// =============================================================================

(function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Number of results per page */
  const PAGE_SIZE = 10;

  /** Debounce delay in milliseconds */
  const DEBOUNCE_DELAY_MS = 1000;

  // ============================================================================
  // DOM Elements
  // ============================================================================

  const searchInput = document.getElementById('userSearch');
  const searchResults = document.getElementById('searchResults');

  if (!searchInput || !searchResults) {
    return;
  }

  // ============================================================================
  // State
  // ============================================================================

  const state = {
    debounceTimer: null,
    currentQuery: '',
    currentPage: 0,
    totalCount: 0,
    loadedUsers: [],
    isLoading: false
  };

  // ============================================================================
  // Utility Functions
  // ============================================================================

  /**
   * Escapes HTML special characters to prevent XSS.
   * @param {string} str - The string to escape.
   * @returns {string} The escaped string.
   */
  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  /**
   * Generates initials from a name.
   * @param {string} name - The full name.
   * @returns {string} The initials (1-2 characters).
   */
  function getInitials(name) {
    const parts = (name || '').trim().split(/\s+/);

    if (parts.length >= 2) {
      const firstInitial = parts[0][0] || '';
      const lastInitial = parts[parts.length - 1][0] || '';
      return (firstInitial + lastInitial).toUpperCase();
    }

    return (name || '?')[0].toUpperCase();
  }

  // ============================================================================
  // Results Panel Functions
  // ============================================================================

  /**
   * Opens the search results panel.
   */
  function openResults() {
    searchResults.classList.add('open');
    searchResults.setAttribute('aria-hidden', 'false');
  }

  /**
   * Closes the search results panel.
   */
  function closeResults() {
    searchResults.classList.remove('open');
    searchResults.setAttribute('aria-hidden', 'true');
  }

  // ============================================================================
  // Rendering Functions
  // ============================================================================

  /**
   * Renders a single user result item.
   * @param {Object} user - The user object.
   * @returns {string} The HTML string.
   */
  function renderUserItem(user) {
    const initials = getInitials(user.name);
    return `
      <a href="/profile/view/${escapeHtml(user.id)}" class="search-result-item">
        <div class="user-avatar">${escapeHtml(initials)}</div>
        <div class="user-info">
          <div class="user-name">${escapeHtml(user.name)}</div>
          <div class="user-email">${escapeHtml(user.email)}</div>
        </div>
      </a>
    `;
  }

  /**
   * Renders the search results.
   */
  function renderResults() {
    if (state.loadedUsers.length === 0 && !state.isLoading) {
      searchResults.innerHTML = '<div class="search-results-empty">No users found</div>';
      openResults();
      return;
    }

    const userItemsHtml = state.loadedUsers.map(renderUserItem).join('');

    // Show load more button if there are more results
    let loadMoreHtml = '';
    if (state.loadedUsers.length < state.totalCount) {
      loadMoreHtml = `
        <div class="search-load-more" id="loadMoreUsers">
          <span class="plus-icon">+</span>
          <span>Load more</span>
        </div>
      `;
    }

    searchResults.innerHTML = userItemsHtml + loadMoreHtml;
    openResults();

    // Attach load more handler
    const loadMoreBtn = document.getElementById('loadMoreUsers');
    if (loadMoreBtn) {
      loadMoreBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        loadMore();
      });
    }
  }

  /**
   * Shows a loading indicator.
   */
  function showLoading() {
    if (state.loadedUsers.length === 0) {
      searchResults.innerHTML = '<div class="search-loading">Searching...</div>';
      openResults();
    }
  }

  /**
   * Shows an error message.
   */
  function showError() {
    searchResults.innerHTML = '<div class="search-results-empty">Search failed</div>';
    openResults();
  }

  // ============================================================================
  // API Functions
  // ============================================================================

  /**
   * Searches for users via the API.
   * @param {string} query - The search query.
   * @param {number} page - The page number.
   */
  async function searchUsers(query, page) {
    if (state.isLoading) {
      return;
    }

    state.isLoading = true;

    if (page === 0) {
      showLoading();
    }

    try {
      const params = new URLSearchParams({
        name: query,
        page: page.toString(),
        pageSize: PAGE_SIZE.toString()
      });

      const response = await fetch(`/api/users/search?${params}`, {
        method: 'GET',
        credentials: 'include'
      });

      if (!response.ok) {
        console.error('[UserSearch] Search failed:', response.status);
        showError();
        return;
      }

      const data = await response.json();
      state.totalCount = data.total || 0;

      if (page === 0) {
        state.loadedUsers = data.items || [];
      } else {
        state.loadedUsers = state.loadedUsers.concat(data.items || []);
      }

      state.currentPage = page;
      renderResults();
    } catch (error) {
      console.error('[UserSearch] Search error:', error);
      showError();
    } finally {
      state.isLoading = false;
    }
  }

  /**
   * Loads more search results.
   */
  function loadMore() {
    if (state.isLoading || state.loadedUsers.length >= state.totalCount) {
      return;
    }

    searchUsers(state.currentQuery, state.currentPage + 1);
  }

  /**
   * Resets the search state.
   */
  function resetSearch() {
    state.currentQuery = '';
    state.currentPage = 0;
    state.totalCount = 0;
    state.loadedUsers = [];
  }

  // ============================================================================
  // Event Handlers
  // ============================================================================

  /**
   * Handles input changes in the search field.
   */
  function handleInput() {
    const query = searchInput.value.trim();

    if (state.debounceTimer) {
      clearTimeout(state.debounceTimer);
    }

    if (!query) {
      closeResults();
      resetSearch();
      return;
    }

    state.debounceTimer = setTimeout(function () {
      if (query !== state.currentQuery) {
        state.currentQuery = query;
        state.currentPage = 0;
        state.totalCount = 0;
        state.loadedUsers = [];
        searchUsers(query, 0);
      }
    }, DEBOUNCE_DELAY_MS);
  }

  /**
   * Handles focus on the search input.
   */
  function handleFocus() {
    if (state.loadedUsers.length > 0 || (state.currentQuery && state.totalCount === 0)) {
      renderResults();
    }
  }

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes search event listeners.
   */
  function initEventListeners() {
    searchInput.addEventListener('input', handleInput);
    searchInput.addEventListener('focus', handleFocus);

    // Close results when clicking outside
    document.addEventListener('click', function (e) {
      if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
        closeResults();
      }
    });

    // Keep results open when clicking inside
    searchResults.addEventListener('click', function (e) {
      e.stopPropagation();
    });
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  initEventListeners();
})();
