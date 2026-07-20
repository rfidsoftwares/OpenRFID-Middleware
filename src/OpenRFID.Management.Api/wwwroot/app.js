document.addEventListener('DOMContentLoaded', () => {
    // --- State Variables ---
    let activeConfig = null;
    let isStreamPaused = false;
    let tagSocket = null;
    let logSocket = null;
    let processedTagCount = 0;
    let storedApiKey = localStorage.getItem('openrfid_api_key') || '';

    // --- DOM Elements ---
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    const wsStatusDot = document.querySelector('#ws-status-pill .status-dot');
    const wsStatusText = document.getElementById('ws-status-text');
    const connectedReadersCountEl = document.getElementById('connected-readers-count');
    const tagsProcessedCountEl = document.getElementById('tags-processed-count');
    const offlineQueueCountEl = document.getElementById('offline-queue-count');

    const telemetryTableBody = document.getElementById('telemetry-table-body');
    const btnPauseStream = document.getElementById('btn-pause-stream');
    const btnClearStream = document.getElementById('btn-clear-stream');
    const btnSimulateTag = document.getElementById('btn-simulate-tag');

    const readersGrid = document.getElementById('readers-grid');
    const btnAddReader = document.getElementById('btn-add-reader');

    const slidingWindowInput = document.getElementById('sliding-window-sec');
    const dailyUniqueToggle = document.getElementById('daily-unique-toggle');
    const minRssiInput = document.getElementById('min-rssi');
    const rssiValSpan = document.getElementById('rssi-val');
    const antennaMaskInput = document.getElementById('antenna-mask');
    const epcRegexInput = document.getElementById('epc-regex');
    const testEpcsInput = document.getElementById('test-epcs-input');
    const regexResultsContainer = document.getElementById('regex-results-container');
    const btnTestRegex = document.getElementById('btn-test-regex');
    const btnSaveFilters = document.getElementById('btn-save-filters');

    const httpMethodSelect = document.getElementById('http-method');
    const targetUrlInput = document.getElementById('target-url');
    const triggerModeSelect = document.getElementById('trigger-mode');
    const templateFormatSelect = document.getElementById('template-format');
    const customTemplateInput = document.getElementById('custom-template');
    const btnPreviewTemplate = document.getElementById('btn-preview-template');
    const templatePreviewOutput = document.getElementById('template-preview-output');
    const btnSaveDispatcher = document.getElementById('btn-save-dispatcher');

    const logConsoleBody = document.getElementById('log-console-body');
    const btnClearLogs = document.getElementById('btn-clear-logs');

    const readerModal = document.getElementById('reader-modal');
    const btnCloseModal = document.getElementById('btn-close-modal');
    const btnCancelModal = document.getElementById('btn-cancel-modal');
    const btnSaveReaderModal = document.getElementById('btn-save-reader-modal');
    const modalReaderId = document.getElementById('modal-reader-id');
    const modalProviderId = document.getElementById('modal-provider-id');
    const modalIp = document.getElementById('modal-ip');
    const modalPort = document.getElementById('modal-port');

    // --- Authenticated fetch helper ---
    async function authFetch(url, options = {}) {
        options.headers = options.headers || {};
        if (storedApiKey) {
            options.headers['X-API-Key'] = storedApiKey;
        }
        return fetch(url, options);
    }

    // --- Init Application ---
    initTabs();
    initWebSockets();
    loadSystemHealth();
    loadConfig();

    setInterval(loadSystemHealth, 3000);

    // --- Tab Navigation ---
    function initTabs() {
        tabBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                tabBtns.forEach(b => b.classList.remove('active'));
                tabContents.forEach(c => c.classList.remove('active'));

                btn.classList.add('active');
                const targetTab = document.getElementById(`tab-${btn.dataset.tab}`);
                if (targetTab) targetTab.classList.add('active');
            });
        });
    }

    // --- System Status & Metrics Fetching ---
    async function loadSystemHealth() {
        try {
            const res = await authFetch('/api/v1/status');
            if (!res.ok) return;
            const health = await res.json();

            connectedReadersCountEl.textContent = `${health.connectedReadersCount}/${health.totalReadersCount}`;
            offlineQueueCountEl.textContent = health.offlineQueueCount.toLocaleString();

            if (processedTagCount === 0) {
                processedTagCount = health.totalFilteredTagsCount;
                tagsProcessedCountEl.textContent = processedTagCount.toLocaleString();
            }
        } catch (err) {
            console.error('Failed to fetch health status:', err);
        }
    }

    // --- Load Configuration ---
    async function loadConfig() {
        try {
            const res = await authFetch('/api/v1/config');
            if (!res.ok) return;
            activeConfig = await res.json();

            // Populate Filter tab
            if (activeConfig.filter) {
                slidingWindowInput.value = activeConfig.filter.slidingWindowSeconds || 5.0;
                dailyUniqueToggle.checked = !!activeConfig.filter.dailyUniqueEnabled;
                minRssiInput.value = activeConfig.filter.minRssiDbm || -75;
                rssiValSpan.textContent = minRssiInput.value;
                antennaMaskInput.value = activeConfig.filter.antennaMask || '';
                epcRegexInput.value = activeConfig.filter.epcRegexPattern || '';
            }

            // Populate Dispatcher tab
            if (activeConfig.dispatch) {
                httpMethodSelect.value = activeConfig.dispatch.httpMethod || 'POST';
                targetUrlInput.value = activeConfig.dispatch.targetUrl || 'http://localhost:8080/api/v1/tags';
                triggerModeSelect.value = activeConfig.dispatch.triggerMode || 'Instant';
                templateFormatSelect.value = activeConfig.dispatch.templateFormat || 'json';
                customTemplateInput.value = activeConfig.dispatch.customTemplate || '';
            }

            // Render Readers
            renderReadersGrid();
        } catch (err) {
            console.error('Failed to load config:', err);
        }
    }

    // --- WebSockets Streaming ---
    function initWebSockets() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const host = window.location.host;
        const authQuery = storedApiKey ? `?apiKey=${encodeURIComponent(storedApiKey)}` : '';

        // Tag WebSocket
        tagSocket = new WebSocket(`${protocol}//${host}/ws/tags${authQuery}`);

        tagSocket.onopen = () => {
            wsStatusDot.className = 'status-dot connected';
            wsStatusText.textContent = 'Live Connected';
            appendLog('Connected to Live Tag WebSocket stream.');
        };

        tagSocket.onclose = () => {
            wsStatusDot.className = 'status-dot disconnected';
            wsStatusText.textContent = 'Disconnected';
            setTimeout(initWebSockets, 3000);
        };

        tagSocket.onmessage = (event) => {
            if (isStreamPaused) return;
            try {
                const data = JSON.parse(event.data);
                if (data.tag) {
                    addTagToTable(data.tag, data.type);
                    if (data.type === 'filtered') {
                        processedTagCount++;
                        tagsProcessedCountEl.textContent = processedTagCount.toLocaleString();
                    }
                }
            } catch (err) {
                console.error('Error parsing tag WS payload:', err);
            }
        };

        // Log WebSocket
        logSocket = new WebSocket(`${protocol}//${host}/ws/logs${authQuery}`);
        logSocket.onmessage = (event) => {
            try {
                const data = JSON.parse(event.data);
                if (data.message) {
                    appendLog(data.message);
                }
            } catch (err) { }
        };
    }

    function addTagToTable(tag, streamType) {
        if (telemetryTableBody.querySelector('.empty-row')) {
            telemetryTableBody.innerHTML = '';
        }

        const tr = document.createElement('tr');
        const rssiPct = Math.max(0, Math.min(100, (tag.rssi + 100) * 1.25));

        const timeStr = new Date(tag.timestampUtc || Date.now()).toLocaleTimeString();

        tr.innerHTML = `
            <td>${timeStr}</td>
            <td class="font-mono" style="color: #38bdf8; font-weight:600;">${tag.epc}</td>
            <td class="font-mono" style="color: #94a3b8;">${tag.tid || '-'}</td>
            <td>${tag.readerId || 'Simulator-01'}</td>
            <td>Port ${tag.antennaPort || 1}</td>
            <td>
                <div class="rssi-pill">
                    <span>${tag.rssi.toFixed(1)} dBm</span>
                    <div class="rssi-bar"><div class="rssi-fill" style="width: ${rssiPct}%"></div></div>
                </div>
            </td>
            <td>${tag.readCount || 1}</td>
            <td><span class="status-badge ${streamType}">${streamType.toUpperCase()}</span></td>
        `;

        telemetryTableBody.insertBefore(tr, telemetryTableBody.firstChild);

        // Keep maximum 50 rows
        if (telemetryTableBody.children.length > 50) {
            telemetryTableBody.removeChild(telemetryTableBody.lastChild);
        }
    }

    function appendLog(msg) {
        const line = document.createElement('div');
        line.className = 'log-line';
        line.textContent = msg;
        logConsoleBody.appendChild(line);
        logConsoleBody.scrollTop = logConsoleBody.scrollHeight;
    }

    // --- UI Controls & Event Listeners ---
    minRssiInput.addEventListener('input', (e) => {
        rssiValSpan.textContent = e.target.value;
    });

    btnPauseStream.addEventListener('click', () => {
        isStreamPaused = !isStreamPaused;
        btnPauseStream.textContent = isStreamPaused ? '▶️ Resume Stream' : '⏸️ Pause Stream';
    });

    btnClearStream.addEventListener('click', () => {
        telemetryTableBody.innerHTML = `
            <tr class="empty-row">
                <td colspan="8">Waiting for incoming RFID tag reads...</td>
            </tr>`;
    });

    btnClearLogs.addEventListener('click', () => {
        logConsoleBody.innerHTML = '';
    });

    btnSimulateTag.addEventListener('click', async () => {
        try {
            await authFetch('/api/v1/simulate/tag', { method: 'POST' });
        } catch (err) {
            console.error('Failed to inject tag:', err);
        }
    });

    // --- Regex Tester ---
    btnTestRegex.addEventListener('click', async () => {
        const pattern = epcRegexInput.value.trim();
        const epcs = testEpcsInput.value.split('\n').map(s => s.trim()).filter(Boolean);

        if (!pattern) {
            regexResultsContainer.textContent = 'Please enter a valid regex pattern.';
            return;
        }

        try {
            const res = await authFetch('/api/v1/filters/test-regex', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ pattern, testEpcs: epcs })
            });

            const data = await res.json();
            if (data.results) {
                regexResultsContainer.innerHTML = data.results.map(r =>
                    `[${r.isMatch ? 'MATCH ✅' : 'REJECT ❌'}] ${r.epc}`
                ).join('\n');
            } else {
                regexResultsContainer.textContent = data.error || 'Error testing regex';
            }
        } catch (err) {
            regexResultsContainer.textContent = 'Failed to execute regex test request.';
        }
    });

    // --- Template Previewer ---
    btnPreviewTemplate.addEventListener('click', async () => {
        const format = templateFormatSelect.value;
        const customTemplate = customTemplateInput.value;

        try {
            const res = await authFetch('/api/v1/templates/preview', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ format, customTemplate })
            });

            const data = await res.json();
            if (data.rendered) {
                templatePreviewOutput.textContent = data.rendered;
            } else {
                templatePreviewOutput.textContent = data.error || 'Error rendering template';
            }
        } catch (err) {
            templatePreviewOutput.textContent = 'Failed to preview template.';
        }
    });

    // --- Save Filter Configuration ---
    btnSaveFilters.addEventListener('click', async () => {
        if (!activeConfig) return;

        activeConfig.filter = {
            slidingWindowSeconds: parseFloat(slidingWindowInput.value) || 5.0,
            dailyUniqueEnabled: dailyUniqueToggle.checked,
            minRssiDbm: parseFloat(minRssiInput.value),
            antennaMask: antennaMaskInput.value ? parseInt(antennaMaskInput.value) : null,
            epcRegexPattern: epcRegexInput.value.trim() || null
        };

        await saveConfigToServer();
    });

    // --- Save Dispatcher Configuration ---
    btnSaveDispatcher.addEventListener('click', async () => {
        if (!activeConfig) return;

        activeConfig.dispatch = {
            httpMethod: httpMethodSelect.value,
            targetUrl: targetUrlInput.value.trim(),
            triggerMode: triggerModeSelect.value,
            templateFormat: templateFormatSelect.value,
            customTemplate: customTemplateInput.value.trim() || null,
            periodicIntervalMs: 5000,
            batchCountThreshold: 100,
            customHeaders: {}
        };

        await saveConfigToServer();
    });

    async function saveConfigToServer() {
        try {
            const res = await authFetch('/api/v1/config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(activeConfig)
            });

            if (res.ok) {
                alert('Configuration updated and hot-reloaded successfully!');
                loadConfig();
            } else {
                const err = await res.json();
                alert(`Error saving configuration: ${err.error}`);
            }
        } catch (err) {
            alert(`Network error saving configuration: ${err.message}`);
        }
    }

    // --- Readers Render & Modal ---
    function renderReadersGrid() {
        if (!activeConfig || !activeConfig.readers) return;

        readersGrid.innerHTML = activeConfig.readers.map(r => `
            <div class="card">
                <h3>${r.brandName || r.readerId}</h3>
                <p><strong>Provider:</strong> ${r.providerId}</p>
                <p><strong>Target:</strong> ${r.ipAddress || r.comPort || '127.0.0.1'}:${r.port || '5084'}</p>
                <p><strong>Reader ID:</strong> ${r.readerId}</p>
                <div style="margin-top: 1rem;">
                    <span class="status-pill"><span class="status-dot connected"></span> Active Profile</span>
                </div>
            </div>
        `).join('');
    }

    btnAddReader.addEventListener('click', () => {
        readerModal.classList.add('open');
    });

    btnCloseModal.addEventListener('click', () => readerModal.classList.remove('open'));
    btnCancelModal.addEventListener('click', () => readerModal.classList.remove('open'));

    btnSaveReaderModal.addEventListener('click', async () => {
        const id = modalReaderId.value.trim() || `Reader-${Date.now()}`;
        const provider = modalProviderId.value;
        const ip = modalIp.value.trim();
        const port = parseInt(modalPort.value) || 5084;

        if (!activeConfig.readers) activeConfig.readers = [];
        activeConfig.readers.push({
            readerId: id,
            providerId: provider,
            brandName: `${provider} Reader`,
            ipAddress: ip,
            port: port,
            healthCheckIntervalMs: 5000
        });

        readerModal.classList.remove('open');
        await saveConfigToServer();
    });
});
