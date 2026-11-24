/**
 * DC_BOUTIQUE — Motor de Captura para Pistolas Escáner de Códigos de Barras y QR
 * Compatible con: Honeywell, Zebra, Netum, Datalogic, Eyoyo y lectores genéricos USB/Bluetooth (HID)
 */
class PosScannerManager {
    constructor(options = {}) {
        this.onScan = options.onScan || function(code) { console.log('Escaneado:', code); };
        this.onError = options.onError || function(err) { console.error('Error escáner:', err); };
        this.minChars = options.minChars || 3;
        this.maxIntervalMs = options.maxIntervalMs || 60; // Típico de lector láser/2D (< 50ms entre caracteres)
        
        this.buffer = '';
        this.lastKeyTime = 0;
        this.isScannerInput = false;

        this.initAudio();
        this.initKeyboardListener();
    }

    /**
     * Sintetizador de audio nativo Web Audio API (efecto 'BEEP' instantáneo de caja registradora)
     */
    initAudio() {
        try {
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            this.audioCtx = new AudioCtx();
        } catch (e) {
            this.audioCtx = null;
        }
    }

    playSuccessBeep() {
        if (!this.audioCtx) return;
        try {
            if (this.audioCtx.state === 'suspended') {
                this.audioCtx.resume();
            }
            const osc = this.audioCtx.createOscillator();
            const gain = this.audioCtx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(1760, this.audioCtx.currentTime); // Tono A6 (1760 Hz) clásico de escáner POS
            gain.gain.setValueAtTime(0.15, this.audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, this.audioCtx.currentTime + 0.12);
            osc.connect(gain);
            gain.connect(this.audioCtx.destination);
            osc.start();
            osc.stop(this.audioCtx.currentTime + 0.12);
        } catch (e) {}
    }

    playErrorBeep() {
        if (!this.audioCtx) return;
        try {
            if (this.audioCtx.state === 'suspended') {
                this.audioCtx.resume();
            }
            const osc = this.audioCtx.createOscillator();
            const gain = this.audioCtx.createGain();
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(220, this.audioCtx.currentTime);
            gain.gain.setValueAtTime(0.2, this.audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, this.audioCtx.currentTime + 0.3);
            osc.connect(gain);
            gain.connect(this.audioCtx.destination);
            osc.start();
            osc.stop(this.audioCtx.currentTime + 0.3);
        } catch (e) {}
    }

    /**
     * Escucha global de eventos de teclado en segundo plano
     */
    initKeyboardListener() {
        window.addEventListener('keydown', (e) => {
            const now = Date.now();
            const timeDiff = now - this.lastKeyTime;

            // Si el foco está en un input de texto normal editable y no es un escaneo rápido, permitir escritura manual
            const activeTag = document.activeElement ? document.activeElement.tagName.toLowerCase() : '';
            const isInputFocused = (activeTag === 'input' || activeTag === 'textarea');
            const isScanBox = document.activeElement && document.activeElement.classList.contains('dc-scanner-input');

            // Enter indica fin de lectura de la pistola escáner
            if (e.key === 'Enter') {
                if (this.buffer.length >= this.minChars && (this.isScannerInput || isScanBox)) {
                    e.preventDefault();
                    e.stopPropagation();
                    const scannedCode = this.buffer.trim();
                    this.buffer = '';
                    this.isScannerInput = false;

                    this.playSuccessBeep();
                    this.onScan(scannedCode);
                    return;
                }
                this.buffer = '';
                this.isScannerInput = false;
                return;
            }

            // Ignorar teclas modificadoras
            if (e.key.length > 1) return;

            // Detección de ráfaga: si los caracteres llegan a menos de 50ms, viene de una pistola de escáner USB
            if (timeDiff <= this.maxIntervalMs) {
                this.isScannerInput = true;
            } else if (!isScanBox) {
                // Si la pausa es larga, reiniciar buffer (escritura humana casual)
                this.buffer = '';
                this.isScannerInput = false;
            }

            this.buffer += e.key;
            this.lastKeyTime = now;
        }, true);
    }
}

// Exponer globalmente
window.PosScannerManager = PosScannerManager;
