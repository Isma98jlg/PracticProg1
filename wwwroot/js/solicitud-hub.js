// WebSocket / SignalR client for Plataforma de Créditos
(function () {
    const userId = document.querySelector('meta[name="user-id"]')?.content ||
        document.querySelector('[data-user-id]')?.dataset.userId ||
        window.__userId || null;

    const statusBadge = document.getElementById('ws-status-badge');
    const hubUrl = '/hubs/solicitudes';

    if (!userId) {
        updateStatus('⚠️ No autenticado', 'bg-secondary');
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: function (retryCount) {
                if (retryCount > 5) return null; // Stop after 5 retries
                return Math.min(1000 * Math.pow(2, retryCount), 30000);
            }
        })
        .build();

    // Connection opened
    connection.onclose(function () {
        updateStatus('🔴 Desconectado', 'bg-danger');
        // Try to reconnect automatically via withAutomaticReconnect
        setTimeout(function () {
            connection.start().then(function () {
                updateStatus('🟢 Conectado', 'bg-success');
                // Request current state on reconnect
                connection.invoke('RequestCurrentState', window._ultimaSolicitudId || 0);
            }).catch(function () { });
        }, 2000);
    });

    // Connection opened
    connection.on("ConnectionAccepted", function (message) {
        updateStatus('🟢 Conectado', 'bg-success');
    });

    // Connection rejected
    connection.on("ConnectionRejected", function (reason) {
        updateStatus('🔴 Rechazado: ' + reason, 'bg-danger');
    });

    // Handle SolicitudEstadoActualizado event
    connection.on("SolicitudEstadoActualizado", function (data) {
        window._ultimaSolicitudId = data.SolicitudId;

        // Show notification toast
        showToast(data);

        // Update page if on the detail view of this solicitud
        if (window._currentSolicitudId === data.SolicitudId) {
            location.reload();
        }
    });

    // Handle EstadoActualizado (from RequestCurrentState)
    connection.on("EstadoActualizado", function (data) {
        showToast(data);
    });

    // Start connection
    connection.start()
        .then(function () {
            updateStatus('🟢 Conectado', 'bg-success');
            // Register to user's group
            console.log('SignalR connected as user group: ' + userId);
        })
        .catch(function (err) {
            console.error('SignalR connection error:', err);
            updateStatus('🔴 Error de conexión', 'bg-danger');
        });

    function updateStatus(text, badgeClass) {
        if (statusBadge) {
            statusBadge.textContent = text;
            statusBadge.className = 'badge rounded-pill ' + badgeClass;
        }
    }

    function showToast(data) {
        // Create a toast notification
        const toastEl = document.createElement('div');
        toastEl.className = 'position-fixed top-0 end-0 p-3';
        toastEl.style.zIndex = '11000';
        toastEl.innerHTML = `
            <div id="liveToast" class="toast show" role="alert">
                <div class="toast-header ${data.Estado === 'Aprobado' ? 'bg-success text-white' : 'bg-danger text-white'}">
                    <strong class="me-auto">Solicitud #${data.SolicitudId}</strong>
                    <small>${new Date().toLocaleTimeString()}</small>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    <strong>Estado:</strong> ${data.Estado}<br>
                    ${data.MotivoRechazo ? `<strong>Motivo:</strong> ${data.MotivoRechazo}` : ''}
                </div>
            </div>
        `;
        document.body.appendChild(toastEl);

        // Auto-remove after 5 seconds
        setTimeout(function () {
            toastEl.remove();
        }, 5000);
    }

    // Expose variables for views
    window._signalrConnection = connection;
    window._signalrUserId = userId;
})();
