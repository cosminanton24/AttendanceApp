document.addEventListener('DOMContentLoaded', () => {
  const root = document.querySelector('.join-page');
  if (!root) return;

  const lectureId = root.getAttribute('data-lecture-id');
  const statusText = document.getElementById('joinStatusText');
  const resultEl = document.getElementById('joinResult');
  const a11yStatus = document.getElementById('joinA11yStatus');
  const a11yProgress = document.getElementById('joinProgressA11y');
  const spinner = root.querySelector('.join-spinner');
  const progressBar = root.querySelector('.join-progress .progress-bar');

  function setResult(message, kind) {
    if (resultEl) {
      resultEl.textContent = message || '';
      resultEl.classList.remove('ok', 'err');
      if (kind === 'ok') resultEl.classList.add('ok');
      if (kind === 'err') resultEl.classList.add('err');
    }
  }

  function stopLoading() {
    try { if (spinner) spinner.style.display = 'none'; } catch (e) { /* ignore */ }
    try {
      if (progressBar) {
        progressBar.classList.remove('progress-bar-animated', 'progress-bar-striped');
      }
    } catch (e) { /* ignore */ }
    try { if (a11yProgress) a11yProgress.value = 100; } catch (e) { /* ignore */ }
  }

  function extractErrorMessage(data, fallback) {
    if (!data) return fallback;
    if (typeof data === 'string') return data;
    if (data.detail) {
      const m = String(data.detail).match(/--\s*(.*?)\s*Severity:/is);
      if (m && m[1]) return m[1].trim();
      return String(data.detail).trim();
    }
    if (data.title) return String(data.title);
    if (data.message) return String(data.message);
    if (data.errors) return JSON.stringify(data.errors);
    return fallback;
  }

  async function tryJoin(url) {
    const resp = await fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Accept': 'application/json' }
    });
    return resp;
  }

  (async () => {
    if (!lectureId) {
      stopLoading();
      if (statusText) statusText.textContent = 'Missing lecture id.';
      try { if (a11yStatus) a11yStatus.value = 'Missing lecture id.'; } catch (e) { /* ignore */ }
      setResult('Invalid lecture id.', 'err');
      return;
    }

    if (statusText) statusText.textContent = 'Joining…';
    try { if (a11yStatus) a11yStatus.value = 'Joining'; } catch (e) { /* ignore */ }
    setResult('', null);

    try { if (a11yProgress) a11yProgress.value = 10; } catch (e) { /* ignore */ }

    const primary = `/api/lectures/join/${encodeURIComponent(lectureId)}`;
    const secondary = `/api/lecture/join/${encodeURIComponent(lectureId)}`;

    let resp;
    try {
      resp = await tryJoin(primary);
      if (resp.status === 404) {
        resp = await tryJoin(secondary);
      }
    } catch (e) {
      stopLoading();
      if (statusText) statusText.textContent = 'Failed.';
      try { if (a11yStatus) a11yStatus.value = 'Failed'; } catch (e) { /* ignore */ }
      setResult('Network error while joining.', 'err');
      return;
    }

    if (!resp.ok) {
      stopLoading();
      if (statusText) statusText.textContent = 'Failed.';
      try { if (a11yStatus) a11yStatus.value = 'Failed'; } catch (e) { /* ignore */ }

      let message = `Error ${resp.status}`;
      try {
        const data = await resp.json();
        message = extractErrorMessage(data, message);
      } catch (e) {
        try {
          const txt = await resp.text();
          if (txt) message = txt;
        } catch (e2) { /* ignore */ }
      }

      setResult(message, 'err');
      return;
    }

    stopLoading();
    if (statusText) statusText.textContent = 'Done.';
    try { if (a11yStatus) a11yStatus.value = 'Joined'; } catch (e) { /* ignore */ }
    setResult('You joined the lecture.', 'ok');

    setTimeout(() => {
      window.location.href = '/home/index';
    }, 3000);
  })();
});
