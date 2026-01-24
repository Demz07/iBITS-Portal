/* 
   FILE PATH: wwwroot/js/landing-page.js
   Professional Landing Page JavaScript for iBITS Portal
*/

// ============================================================
// INITIALIZE AOS (Animate On Scroll)
// ============================================================
document.addEventListener('DOMContentLoaded', function () {
    AOS.init({
        duration: 800,
        easing: 'ease-out',
        once: true,
        offset: 100,
        delay: 100
    });
});

// ============================================================
// FLOATING NAVIGATION BAR
// ============================================================
const floatingNav = document.getElementById('floatingNav');
let lastScrollTop = 0;
const scrollThreshold = 100;

window.addEventListener('scroll', function () {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

    if (scrollTop > scrollThreshold) {
        floatingNav.classList.add('visible');
    } else {
        floatingNav.classList.remove('visible');
    }

    lastScrollTop = scrollTop;
});

// ============================================================
// SMOOTH SCROLL FOR NAVIGATION LINKS
// ============================================================
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));

        if (target) {
            const offsetTop = target.offsetTop - 80; // Account for fixed nav height
            window.scrollTo({
                top: offsetTop,
                behavior: 'smooth'
            });
        }
    });
});

// ============================================================
// ANIMATED COUNTER FOR STATISTICS
// ============================================================
const statNumbers = document.querySelectorAll('.stat-number');
let hasCounted = false;

function animateCounter(element, target, duration = 2000) {
    const start = 0;
    const increment = target / (duration / 16); // 60 FPS
    let current = start;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            element.textContent = Math.floor(target);
            clearInterval(timer);
        } else {
            element.textContent = Math.floor(current);
        }
    }, 16);
}

// Intersection Observer for Statistics Section
const statsSection = document.querySelector('.statistics-section');
if (statsSection) {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting && !hasCounted) {
                hasCounted = true;
                statNumbers.forEach(stat => {
                    const target = parseInt(stat.getAttribute('data-target'));
                    animateCounter(stat, target, 2000);
                });
            }
        });
    }, { threshold: 0.5 });

    observer.observe(statsSection);
}

// ============================================================
// MATRIX RAIN ANIMATION (1s and 0s)
// ============================================================
const canvas = document.getElementById('matrixCanvas');
const ctx = canvas.getContext('2d');

// Set canvas size
function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

window.addEventListener('resize', resizeCanvas);
resizeCanvas();

// Matrix Rain Configuration
const fontSize = 26; // Size of the characters
const columns = canvas.width / fontSize;
const drops = [];

// Initialize drops for each column
for (let x = 0; x < columns; x++) {
    drops[x] = 1;
}

// Only 1s and 0s
const characters = "01";

// Draw Matrix Rain
function drawMatrix() {
    // Fade effect for trailing
    ctx.fillStyle = 'rgba(3, 10, 22, 0.05)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Set text style
    ctx.fillStyle = '#D4AF37'; // Gold color
    ctx.font = fontSize + 'px monospace';

    // Draw characters
    for (let i = 0; i < drops.length; i++) {
        // Randomly select 0 or 1
        const text = characters.charAt(Math.floor(Math.random() * characters.length));

        // Draw the character
        ctx.fillText(text, i * fontSize, drops[i] * fontSize);

        // Reset drop to top randomly after reaching bottom
        if (drops[i] * fontSize > canvas.height && Math.random() > 0.975) {
            drops[i] = 0;
        }

        // Move drop down
        drops[i]++;
    }
}

// Animation loop - 33ms for ~30 FPS (smooth matrix effect)
setInterval(drawMatrix, 33);

// ============================================================
// HERO LOGO ANIMATION - SIMPLE BLUR EFFECT
// ============================================================
const heroLogo = document.querySelector('.hero-logo');
const heroSection = document.querySelector('.hero-section');

if (heroLogo && heroSection) {
    window.addEventListener('scroll', () => {
        const scrolled = window.pageYOffset;
        const heroHeight = heroSection.offsetHeight;
        const scrollProgress = Math.min(scrolled / heroHeight, 1);

        if (scrollProgress < 1) {
            // Simple blur effect as you scroll
            const blurAmount = scrollProgress * 15; // Max 15px blur
            heroLogo.style.filter = `blur(${blurAmount}px) drop-shadow(0 20px 50px rgba(0, 0, 0, 0.7))`;
        } else {
            // Keep blurred when past hero section
            heroLogo.style.filter = 'blur(15px) drop-shadow(0 20px 50px rgba(0, 0, 0, 0.7))';
        }
    });
}

// ============================================================
// FEATURE CARDS TILT EFFECT (OPTIONAL)
// ============================================================
const featureCards = document.querySelectorAll('.feature-card');

featureCards.forEach(card => {
    card.addEventListener('mousemove', (e) => {
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const centerX = rect.width / 2;
        const centerY = rect.height / 2;

        const rotateX = (y - centerY) / 20;
        const rotateY = (centerX - x) / 20;

        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateY(-8px)`;
    });

    card.addEventListener('mouseleave', () => {
        card.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) translateY(0)';
    });
});

// ============================================================
// ACTIVE NAV LINK INDICATOR
// ============================================================
const sections = document.querySelectorAll('section[id]');
const navLinks = document.querySelectorAll('.nav-links a');

function setActiveLink() {
    let currentSection = '';
    const scrollPosition = window.pageYOffset + 200;

    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionHeight = section.offsetHeight;

        if (scrollPosition >= sectionTop && scrollPosition < sectionTop + sectionHeight) {
            currentSection = section.getAttribute('id');
        }
    });

    navLinks.forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('href') === `#${currentSection}`) {
            link.classList.add('active');
        }
    });
}

window.addEventListener('scroll', setActiveLink);

// ============================================================
// PERFORMANCE OPTIMIZATION: LAZY LOADING
// ============================================================
if ('IntersectionObserver' in window) {
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                if (img.dataset.src) {
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                }
                observer.unobserve(img);
            }
        });
    });

    // Observe all images with data-src attribute
    document.querySelectorAll('img[data-src]').forEach(img => {
        imageObserver.observe(img);
    });
}

// ============================================================
// SCROLL PROGRESS INDICATOR (OPTIONAL)
// ============================================================
function updateScrollProgress() {
    const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
    const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    const scrolled = (winScroll / height) * 100;

    // If you want to add a progress bar, uncomment and add HTML element
    // const progressBar = document.getElementById('scrollProgress');
    // if (progressBar) {
    //     progressBar.style.width = scrolled + '%';
    // }
}

window.addEventListener('scroll', updateScrollProgress);

// ============================================================
// CONSOLE WELCOME MESSAGE
// ============================================================
console.log('%c🎓 Welcome to iBITS Portal!', 'color: #D4AF37; font-size: 20px; font-weight: bold;');
console.log('%cUnified Attendance & Payment Management System', 'color: #94a3b8; font-size: 14px;');
console.log('%cPowered by ASP.NET Core', 'color: #94a3b8; font-size: 12px;');

// ============================================================
// PREVENT CONTEXT MENU ON IMAGES (OPTIONAL PROTECTION)
// ============================================================
document.querySelectorAll('img').forEach(img => {
    img.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        return false;
    });
});

// ============================================================
// DETECT MOBILE DEVICE
// ============================================================
function isMobileDevice() {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
}

if (isMobileDevice()) {
    document.body.classList.add('mobile-device');
    // Disable tilt effect on mobile for performance
    featureCards.forEach(card => {
        card.removeEventListener('mousemove', () => { });
    });
}

// ============================================================
// INITIALIZE SMOOTH REVEAL FOR ALL SECTIONS
// ============================================================
window.addEventListener('load', () => {
    document.body.classList.add('loaded');

    // Small delay to ensure everything is rendered
    setTimeout(() => {
        AOS.refresh();
    }, 100);
});

// ============================================================
// KEYBOARD NAVIGATION ACCESSIBILITY
// ============================================================
document.addEventListener('keydown', (e) => {
    // Press 'H' to go to home
    if (e.key === 'h' || e.key === 'H') {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Press 'T' to go to top
    if (e.key === 't' || e.key === 'T') {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
});

// ============================================================
// HANDLE WINDOW VISIBILITY (PAUSE ANIMATIONS WHEN TAB HIDDEN)
// ============================================================
let isVisible = true;

document.addEventListener('visibilitychange', () => {
    isVisible = !document.hidden;
    // You can pause/resume animations here if needed for performance
});

// ============================================================
// ENHANCED INTERACTIVE EFFECTS
// ============================================================

// Add click effect on interactive stat cards
document.querySelectorAll('.interactive-stat').forEach(card => {
    card.addEventListener('click', function () {
        const statNumber = this.querySelector('.stat-number');
        const originalColor = statNumber.style.color;

        // Flash effect
        statNumber.style.transition = 'all 0.3s ease';
        statNumber.style.transform = 'scale(1.2)';
        statNumber.style.color = '#F9E076';

        setTimeout(() => {
            statNumber.style.transform = 'scale(1)';
            statNumber.style.color = originalColor || '#fff';
        }, 300);
    });
});

// Add interactive glow effect on feature cards
document.querySelectorAll('.interactive-hover').forEach(card => {
    card.addEventListener('mouseenter', function () {
        this.style.boxShadow = '0 30px 80px rgba(212, 175, 55, 0.3)';
    });

    card.addEventListener('mouseleave', function () {
        this.style.boxShadow = '';
    });
});

// Add click ripple effect on CTA buttons
document.querySelectorAll('.btn-primary-cta, .btn-cta-large').forEach(button => {
    button.addEventListener('click', function (e) {
        const ripple = document.createElement('span');
        const rect = this.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = e.clientX - rect.left - size / 2;
        const y = e.clientY - rect.top - size / 2;

        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = x + 'px';
        ripple.style.top = y + 'px';
        ripple.style.position = 'absolute';
        ripple.style.borderRadius = '50%';
        ripple.style.background = 'rgba(255, 255, 255, 0.5)';
        ripple.style.transform = 'scale(0)';
        ripple.style.animation = 'ripple 0.6s ease-out';
        ripple.style.pointerEvents = 'none';

        this.style.position = 'relative';
        this.style.overflow = 'hidden';
        this.appendChild(ripple);

        setTimeout(() => ripple.remove(), 600);
    });
});

// Add CSS for ripple animation
const style = document.createElement('style');
style.textContent = `
    @keyframes ripple {
        to {
            transform: scale(4);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);

// ============================================================
// DEBUG MODE (Remove in production)
// ============================================================
const DEBUG = false; // Set to true for development

if (DEBUG) {
    console.log('Matrix columns:', drops.length);
    console.log('Canvas size:', canvas.width, 'x', canvas.height);
    console.log('Mobile device:', isMobileDevice());
    console.log('Matrix font size:', fontSize);

    // Show section boundaries
    sections.forEach(section => {
        console.log(`Section ${section.id}:`, section.offsetTop, 'to', section.offsetTop + section.offsetHeight);
    });
}
