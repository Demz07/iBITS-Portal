/* gateway.js */

const canvas = document.getElementById('matrixCanvas');
const ctx = canvas.getContext('2d');

// Set canvas to full screen
function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}
window.addEventListener('resize', resizeCanvas);
resizeCanvas();

// Configuration
const fontSize = 30;
const columns = canvas.width / fontSize;
const drops = []; // Array of drops - one per column

// Initialize drops
for (let x = 0; x < columns; x++) {
    drops[x] = 1;
}

const characters = "01"; // The Matrix Code

function draw() {
    // Translucent background to show trail effect
    // Uses your Deep Navy color (#030a16) with very low opacity
    ctx.fillStyle = 'rgba(3, 10, 22, 0.05)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Set text color to your Theme Gold (#D4AF37)
    ctx.fillStyle = '#D4AF37';
    ctx.font = fontSize + 'px monospace';

    for (let i = 0; i < drops.length; i++) {
        // Pick a random 0 or 1
        const text = characters.charAt(Math.floor(Math.random() * characters.length));

        // Draw the text
        ctx.fillText(text, i * fontSize, drops[i] * fontSize);

        // Reset drop to top randomly or keep falling
        if (drops[i] * fontSize > canvas.height && Math.random() > 0.975) {
            drops[i] = 0;
        }

        drops[i]++;
    }
}

// Run animation
setInterval(draw, 33);

// Mobile Menu Functionality
const hamburger = document.getElementById('landingHamburger');
const mobileMenu = document.getElementById('landingMobileMenu');
const menuClose = document.getElementById('mobileMenuClose');
const menuBackdrop = document.getElementById('menuBackdrop');

function openMobileMenu() {
    hamburger.classList.add('active');
    mobileMenu.classList.add('active');
    menuBackdrop.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeMobileMenu() {
    hamburger.classList.remove('active');
    mobileMenu.classList.remove('active');
    menuBackdrop.classList.remove('active');
    document.body.style.overflow = '';
}

if (hamburger) hamburger.addEventListener('click', openMobileMenu);
if (menuClose) menuClose.addEventListener('click', closeMobileMenu);
if (menuBackdrop) menuBackdrop.addEventListener('click', closeMobileMenu);

document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeMobileMenu();
});