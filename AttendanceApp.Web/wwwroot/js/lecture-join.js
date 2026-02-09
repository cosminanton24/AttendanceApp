/**
 * @fileoverview Lecture join page functionality.
 * Handles the automatic joining of a lecture when the page loads.
 */

'use strict';

(function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Regular expression pattern for validating GUID format */
  const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

  /** Delay before redirecting to home after successful join (in milliseconds) */
  const REDIRECT_DELAY_MS = 3000;

  /** Home page URL for redirect after successful join */
  const HOME_URL = '/home/index';

  /** Result types for UI state */
  const ResultType = Object.freeze({
    OK: 'ok',
    ERROR: 'err'
  });

  /** Status messages */
  const Messages = Object.freeze({
    MISSING_ID: 'Missing lecture id.',
    INVALID_ID: 'Invalid lecture id.',
    ID_NOT_VALID: 'Id is not valid.',
    JOINING: 'Joining…',
    FAILED: 'Failed.',
    DONE: 'Done.',
    JOINED: 'Joined',
    NETWORK_ERROR: 'Network error while joining.',
    SUCCESS: 'You joined the lecture.',
    GEOLOCATION_REQUIRED: 'Geolocation enabled is required.'
  });

  // ============================================================================
  // DOM Elements
  // ============================================================================

  /**
   * Retrieves all required DOM elements for the join page.
   * @returns {Object|null} Object containing DOM elements, or null if root not found.
   */
  function getDomElements() {
    const root = document.querySelector('.join-page');
    if (!root) {
      return null;
    }

    return {
      root,
      lectureId: root.getAttribute('data-lecture-id'),
      isLectureIdValid: root.getAttribute('data-lecture-id-valid') === 'true',
      statusText: document.getElementById('joinStatusText'),
      resultEl: document.getElementById('joinResult'),
      actionsEl: document.getElementById('joinActions'),
      a11yStatus: document.getElementById('joinA11yStatus'),
      a11yProgress: document.getElementById('joinProgressA11y'),
      spinner: root.querySelector('.join-spinner'),
      progressBar: root.querySelector('.join-progress .progress-bar')
    };
  }

  // ============================================================================
  // Utility Functions
  // ============================================================================

  /**
   * Validates if a string is a valid GUID format.
   * @param {string} value - The value to validate.
   * @returns {boolean} True if valid GUID format, false otherwise.
   */
  function isValidGuid(value) {
    return Boolean(value) && GUID_PATTERN.test(value);
  }

  /**
   * Safely sets a value on an element property.
   * @param {HTMLElement|null} element - The target element.
   * @param {string} property - The property name to set.
   * @param {*} value - The value to assign.
   */
  function safeSetProperty(element, property, value) {
    if (element) {
      try {
        element[property] = value;
      } catch {
        // Element may not support the property
      }
    }
  }

  /**
   * Extracts a user-friendly error message from API response data.
   * @param {Object|string|null} data - The response data.
   * @param {string} fallback - Fallback message if extraction fails.
   * @returns {string} The extracted or fallback error message.
   */
  function extractErrorMessage(data, fallback) {
    if (!data) {
      return fallback;
    }

    if (typeof data === 'string') {
      return data;
    }

    if (data.detail) {
      // Try to extract message between '--' and 'Severity:' for validation errors
      const match = String(data.detail).match(/--\s*(.*?)\s*Severity:/is);
      if (match && match[1]) {
        return match[1].trim();
      }
      return String(data.detail).trim();
    }

    if (data.title) {
      return String(data.title);
    }

    if (data.message) {
      return String(data.message);
    }

    if (data.errors) {
      return JSON.stringify(data.errors);
    }

    return fallback;
  }

  // ============================================================================
  // UI Update Functions
  // ============================================================================

  /**
   * Creates UI update functions bound to the DOM elements.
   * @param {Object} elements - The DOM elements object.
   * @returns {Object} Object containing UI update functions.
   */
  function createUiUpdater(elements) {
    const { resultEl, actionsEl, spinner, progressBar, a11yProgress, statusText, a11yStatus } = elements;

    return {
      /**
       * Updates the result display with a message and visual state.
       * @param {string} message - The message to display.
       * @param {string|null} kind - The result type ('ok', 'err', or null).
       */
      setResult(message, kind) {
        if (resultEl) {
          resultEl.textContent = message || '';
          resultEl.classList.remove(ResultType.OK, ResultType.ERROR);

          if (kind === ResultType.OK) {
            resultEl.classList.add(ResultType.OK);
          } else if (kind === ResultType.ERROR) {
            resultEl.classList.add(ResultType.ERROR);
          }
        }

        if (actionsEl) {
          actionsEl.hidden = kind !== ResultType.ERROR;
        }
      },

      /**
       * Stops all loading indicators.
       */
      stopLoading() {
        if (spinner) {
          spinner.style.display = 'none';
        }

        if (progressBar) {
          progressBar.classList.remove('progress-bar-animated', 'progress-bar-striped');
        }

        safeSetProperty(a11yProgress, 'value', 100);
      },

      /**
       * Updates the status text display.
       * @param {string} text - The status text to display.
       */
      setStatusText(text) {
        if (statusText) {
          statusText.textContent = text;
        }
        safeSetProperty(a11yStatus, 'value', text);
      },

      /**
       * Updates the accessibility progress value.
       * @param {number} value - The progress value (0-100).
       */
      setProgress(value) {
        safeSetProperty(a11yProgress, 'value', value);
      }
    };
  }

  // ============================================================================
  // API Functions
  // ============================================================================

  /**
   * Attempts to join a lecture via the API.
   * @param {string} url - The API endpoint URL.
   * @returns {Promise<Response>} The fetch response.
   */
  async function postJoinRequest(url) {
    return fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Accept': 'application/json' }
    });
  }

  /**
   * Builds the API endpoint URLs for joining a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {Object} Object containing primary and fallback URLs.
   */
  function buildJoinUrls(lectureId, position) {
    const encodedId = encodeURIComponent(lectureId);
    const pos = typeof position === 'string' && position ? `?pos=${encodeURIComponent(position)}` : '';
    return {
      primary: `/api/lectures/join/${encodedId}${pos}`
    };
  }

  // ============================================================================
  // Main Join Logic
  // ============================================================================

  /**
   * Handles the join process failure.
   * @param {Object} ui - The UI updater object.
   * @param {string} message - The error message to display.
   */
  function handleJoinFailure(ui, message) {
    ui.stopLoading();
    ui.setStatusText(Messages.FAILED);
    ui.setResult(message, ResultType.ERROR);
  }

  /**
   * Handles successful join.
   * @param {Object} ui - The UI updater object.
   */
  function handleJoinSuccess(ui) {
    ui.stopLoading();
    ui.setStatusText(Messages.DONE);
    ui.setResult(Messages.SUCCESS, ResultType.OK);

    setTimeout(function () {
      window.location.href = HOME_URL;
    }, REDIRECT_DELAY_MS);
  }

  /**
   * Attempts to parse error message from response.
   * @param {Response} resp - The fetch response.
   * @returns {Promise<string>} The extracted error message.
   */
  async function parseErrorFromResponse(resp) {
    const defaultMessage = `Error ${resp.status}`;

    try {
      const data = await resp.json();
      return extractErrorMessage(data, defaultMessage);
    } catch {
      try {
        const text = await resp.text();
        return text || defaultMessage;
      } catch {
        return defaultMessage;
      }
    }
  }

  /**
   * Executes the join process.
   * @param {Object} elements - The DOM elements object.
   */
  async function executeJoin(elements) {
    const ui = createUiUpdater(elements);
    const { lectureId, isLectureIdValid } = elements;

    // Validate lecture ID presence
    if (!lectureId) {
      handleJoinFailure(ui, Messages.INVALID_ID);
      ui.setStatusText(Messages.MISSING_ID);
      return;
    }

    // Validate lecture ID format
    if (!isLectureIdValid || !isValidGuid(lectureId)) {
      handleJoinFailure(ui, Messages.ID_NOT_VALID);
      return;
    }

    // Start join process
    ui.setStatusText(Messages.JOINING);
    ui.setResult('', null);
    ui.setProgress(10);

    // Build API URLs
    const urls = buildJoinUrls(lectureId, elements.studentLocationConcat);

    // Attempt to join
    let response;
    try {
      response = await postJoinRequest(urls.primary);

      // Try fallback URL if primary returns 404
      if (response.status === 404) {
        response = await postJoinRequest(urls.fallback);
      }
    } catch {
      handleJoinFailure(ui, Messages.NETWORK_ERROR);
      return;
    }

    // Handle non-success response
    if (!response.ok) {
      const errorMessage = await parseErrorFromResponse(response);
      handleJoinFailure(ui, errorMessage);
      return;
    }

    // Success
    handleJoinSuccess(ui);
  }

  async function getStudentLocation() {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) return reject(new Error("No geolocation support"));
      navigator.geolocation.getCurrentPosition(resolve, reject, {
        enableHighAccuracy: true,
        timeout: 8000,
        maximumAge: 0
      });
    });
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  /**
   * Initializes the lecture join page.
   */
  async function init() {
    const elements = getDomElements();
    if (!elements) {
      return;
    }

    const ui = createUiUpdater(elements);

    try {
      const pos = await getStudentLocation();
      const { latitude, longitude, accuracy } = pos.coords;
      elements.studentLocation = { latitude, longitude, accuracy };
      elements.studentLocationConcat = `${latitude},${longitude},${accuracy}`;
    } catch {
      ui.stopLoading();
      ui.setStatusText(Messages.FAILED);
      ui.setResult(Messages.GEOLOCATION_REQUIRED, ResultType.ERROR);
      return;
    }

    executeJoin(elements);
  }

  // Start when DOM is ready
  document.addEventListener('DOMContentLoaded', init);
})();
