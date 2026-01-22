/* ProfileSetup.js */
/* Handles file upload, webcam capture, form validation, and matrix background */

document.addEventListener('DOMContentLoaded', function () {
    
    // ============================================================
    // MATRIX BACKGROUND ANIMATION
    // ============================================================
    const canvas = document.getElementById('matrixCanvas');
    const ctx = canvas.getContext('2d');

    // Set canvas size
    function resizeCanvas() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);

    // Matrix characters (mix of characters for tech effect)
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%^&*()iBITS';
    const charArray = chars.split('');
    const fontSize = 14;
    const columns = canvas.width / fontSize;

    // Array to track y position of each column
    const drops = [];
    for (let i = 0; i < columns; i++) {
        drops[i] = Math.random() * -100;
    }

    // Gold color for matrix (matching theme)
    const matrixColor = 'rgba(212, 175, 55, 0.8)';

    function drawMatrix() {
        // Fade effect
        ctx.fillStyle = 'rgba(3, 10, 22, 0.05)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Set text style
        ctx.fillStyle = matrixColor;
        ctx.font = fontSize + 'px monospace';

        // Draw characters
        for (let i = 0; i < drops.length; i++) {
            // Random character
            const char = charArray[Math.floor(Math.random() * charArray.length)];
            
            // Draw character
            ctx.fillText(char, i * fontSize, drops[i] * fontSize);

            // Reset drop to top with random delay
            if (drops[i] * fontSize > canvas.height && Math.random() > 0.975) {
                drops[i] = 0;
            }
            
            // Move drop down
            drops[i]++;
        }
    }

    // Run matrix animation
    setInterval(drawMatrix, 50);

    // ============================================================
    // FILE INPUT HANDLER
    // ============================================================
    const fileInput = document.getElementById('file-input');
    const photoPreview = document.getElementById('photo-preview');
    const placeholder = document.getElementById('placeholder');
    const previewContainer = document.getElementById('preview-container');
    const submitBtn = document.getElementById('btn-submit');

    fileInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            // Validate file type
            const validTypes = ['image/jpeg', 'image/png'];
            if (!validTypes.includes(file.type)) {
                showAlert('Please select a valid image file (JPG or PNG)', 'error');
                fileInput.value = '';
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                showAlert('File size must be less than 5MB', 'error');
                fileInput.value = '';
                return;
            }

            // Show preview
            const reader = new FileReader();
            reader.onload = function (e) {
                photoPreview.src = e.target.result;
                photoPreview.classList.add('visible');
                placeholder.classList.add('hidden');
                previewContainer.classList.add('has-image');
                submitBtn.disabled = false;
            };
            reader.readAsDataURL(file);
        }
    });

    // ============================================================
    // WEBCAM FUNCTIONALITY
    // ============================================================
    const webcamModal = document.getElementById('webcam-modal');
    const webcamVideo = document.getElementById('webcam-video');
    const webcamCanvas = document.getElementById('webcam-canvas');
    const btnWebcam = document.getElementById('btn-webcam');
    const btnCapture = document.getElementById('btn-capture');
    const btnCloseWebcam = document.getElementById('btn-close-webcam');

    let stream = null;

    btnWebcam.addEventListener('click', async function () {
        try {
            stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 640 },
                    height: { ideal: 480 },
                    facingMode: 'user'
                }
            });
            webcamVideo.srcObject = stream;
            webcamModal.classList.add('active');
            document.body.style.overflow = 'hidden';
        } catch (err) {
            showAlert('Could not access camera. Please use file upload instead.', 'error');
            console.error('Camera error:', err);
        }
    });

    btnCloseWebcam.addEventListener('click', function () {
        closeWebcam();
    });

    // Close modal on backdrop click
    webcamModal.addEventListener('click', function (e) {
        if (e.target === webcamModal) {
            closeWebcam();
        }
    });

    // Close modal on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && webcamModal.classList.contains('active')) {
            closeWebcam();
        }
    });

    btnCapture.addEventListener('click', function () {
        // Set canvas dimensions
        webcamCanvas.width = webcamVideo.videoWidth;
        webcamCanvas.height = webcamVideo.videoHeight;

        // Draw video frame to canvas (flip horizontally to match preview)
        const captureCtx = webcamCanvas.getContext('2d');
        captureCtx.translate(webcamCanvas.width, 0);
        captureCtx.scale(-1, 1);
        captureCtx.drawImage(webcamVideo, 0, 0);

        // Convert to blob and set as file input
        webcamCanvas.toBlob(function (blob) {
            const file = new File([blob], 'webcam-photo.jpg', { type: 'image/jpeg' });
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(file);
            fileInput.files = dataTransfer.files;

            // Update preview
            photoPreview.src = webcamCanvas.toDataURL('image/jpeg');
            photoPreview.classList.add('visible');
            placeholder.classList.add('hidden');
            previewContainer.classList.add('has-image');
            submitBtn.disabled = false;

            closeWebcam();
            showAlert('Photo captured successfully!', 'success');
        }, 'image/jpeg', 0.9);
    });

    function closeWebcam() {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
        webcamModal.classList.remove('active');
        document.body.style.overflow = '';
    }

    // ============================================================
    // FORM VALIDATION & SUBMIT
    // ============================================================
    const profileForm = document.getElementById('profile-form');

    profileForm.addEventListener('submit', function (e) {
        if (!fileInput.files || fileInput.files.length === 0) {
            e.preventDefault();
            showAlert('Please select or capture a profile photo', 'error');
            return false;
        }

        // Show loading state
        submitBtn.disabled = true;
        submitBtn.classList.add('loading');
        submitBtn.innerHTML = '<i class="bi bi-arrow-repeat spinner"></i><span class="btn-text">Uploading...</span>';
    });

    // ============================================================
    // ALERT FUNCTION
    // ============================================================
    function showAlert(message, type) {
        // Remove existing alerts
        const existingAlert = document.querySelector('.custom-alert');
        if (existingAlert) {
            existingAlert.remove();
        }

        // Create alert element
        const alert = document.createElement('div');
        alert.className = 'custom-alert custom-alert-' + type;
        
        const iconClass = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
        alert.innerHTML = '<i class="bi ' + iconClass + '"></i><span>' + message + '</span>';

        // Add styles
        alert.style.cssText = 'position: fixed; top: 30px; right: 30px; padding: 16px 24px; border-radius: 12px; display: flex; align-items: center; gap: 12px; font-size: 0.95rem; font-weight: 500; z-index: 2000; animation: slideIn 0.4s ease; backdrop-filter: blur(10px); box-shadow: 0 10px 40px rgba(0,0,0,0.3);';

        if (type === 'success') {
            alert.style.background = 'rgba(16, 185, 129, 0.15)';
            alert.style.border = '1px solid rgba(16, 185, 129, 0.3)';
            alert.style.color = '#10b981';
        } else {
            alert.style.background = 'rgba(239, 68, 68, 0.15)';
            alert.style.border = '1px solid rgba(239, 68, 68, 0.3)';
            alert.style.color = '#ef4444';
        }

        // Add animation keyframes if not exists
        if (!document.getElementById('alert-styles')) {
            const style = document.createElement('style');
            style.id = 'alert-styles';
            style.textContent = '@keyframes slideIn { from { opacity: 0; transform: translateX(100px); } to { opacity: 1; transform: translateX(0); } } @keyframes slideOut { from { opacity: 1; transform: translateX(0); } to { opacity: 0; transform: translateX(100px); } }';
            document.head.appendChild(style);
        }

        document.body.appendChild(alert);

        // Auto remove after 4 seconds
        setTimeout(function() {
            alert.style.animation = 'slideOut 0.4s ease forwards';
            setTimeout(function() { alert.remove(); }, 400);
        }, 4000);
    }
});
