(function () {
  const joinState = document.getElementById("joinState");
  const inClassState = document.getElementById("inClassState");

  const classNameEl = document.getElementById("className");
  const classDescEl = document.getElementById("classDesc");

  const joinBtn = document.getElementById("joinClassBtn");
  const classIdInput = document.getElementById("classIdInput");

  // ------------------------
  // MOCK FETCHES
  // ------------------------

  function fetchCurrentClass() {
    // toggle this to simulate states
    const IS_IN_CLASS = false;

    return new Promise((resolve) => {
      setTimeout(() => {
        if (IS_IN_CLASS) {
          resolve({
            inClass: true,
            name: "Algorithms 101",
            description: "Sorting, graphs, and complexity analysis"
          });
        } else {
          resolve({ inClass: false });
        }
      }, 500);
    });
  }

  function fetchJoinClass(classId) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        if (!classId || classId.length < 4) {
          reject("Invalid class ID");
          return;
        }

        resolve({
          name: "Databases",
          description: "SQL, indexing, and transactions"
        });
      }, 600);
    });
  }

  // ------------------------
  // UI HELPERS
  // ------------------------

  function showJoin() {
    joinState.hidden = false;
    inClassState.hidden = true;
  }

  function showInClass(data) {
    classNameEl.textContent = data.name;
    classDescEl.textContent = data.description || "";
    joinState.hidden = true;
    inClassState.hidden = false;
  }

  // ------------------------
  // INIT
  // ------------------------

  fetchCurrentClass()
    .then((res) => {
      if (res.inClass) {
        showInClass(res);
      } else {
        showJoin();
      }
    })
    .catch(() => showJoin());

  // ------------------------
  // JOIN HANDLER
  // ------------------------

  joinBtn?.addEventListener("click", () => {
    const classId = classIdInput.value.trim();

    fetchJoinClass(classId)
      .then(showInClass)
      .catch((err) => alert(err));
  });
})();
