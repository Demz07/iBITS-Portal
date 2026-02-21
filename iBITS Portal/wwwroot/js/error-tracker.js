// Global Error Tracker for iBITS Portal
// Captures ALL JavaScript errors, AJAX failures, and resource loading issues

(function() {
    'use strict';
    
    console.log('🔍 Error Tracker Initialized - Monitoring all errors...');

    // Track all errors
    const errors = [];
    
    // 1. GLOBAL JAVASCRIPT ERROR HANDLER
    window.addEventListener('error', function(event) {
        const error = {
            type: 'JavaScript Error',
            message: event.message,
            source: event.filename,
            line: event.lineno,
            column: event.colno,
            stack: event.error ? event.error.stack : 'No stack trace',
            timestamp: new Date().toISOString(),
            url: window.location.href
        };
        
        errors.push(error);
        
        console.error('❌ JavaScript Error Detected:', error);
        console.error('Full Error Object:', event.error);
        
        // Send to server
        logErrorToServer(error);
        
        return false;
    }, true);

    // 2. UNHANDLED PROMISE REJECTION HANDLER
    window.addEventListener('unhandledrejection', function(event) {
        const error = {
            type: 'Unhandled Promise Rejection',
            message: event.reason ? event.reason.message || event.reason : 'Unknown rejection',
            stack: event.reason ? event.reason.stack : 'No stack trace',
            timestamp: new Date().toISOString(),
            url: window.location.href
        };
        
        errors.push(error);
        
        console.error('❌ Promise Rejection Detected:', error);
        console.error('Rejection Reason:', event.reason);
        
        logErrorToServer(error);
        
        return false;
    });

    // 3. RESOURCE LOADING ERROR HANDLER (Images, CSS, JS files)
    window.addEventListener('error', function(event) {
        if (event.target !== window) {
            const error = {
                type: 'Resource Loading Error',
                resource: event.target.tagName || 'Unknown',
                src: event.target.src || event.target.href || 'Unknown',
                timestamp: new Date().toISOString(),
                url: window.location.href
            };
            
            errors.push(error);
            
            console.error('❌ Resource Failed to Load:', error);
            
            // Check if it's an image
            if (event.target.tagName === 'IMG') {
                console.error('🖼️ IMAGE LOADING FAILED:', event.target.src);
                console.error('Alt text:', event.target.alt);
                console.error('Parent element:', event.target.parentElement);
            }
            
            logErrorToServer(error);
        }
    }, true);

    // 4. AJAX/FETCH ERROR TRACKING
    // Override fetch to track failures
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        console.log('🌐 Fetch Request:', args[0]);
        
        return originalFetch.apply(this, args)
            .then(response => {
                if (!response.ok) {
                    const error = {
                        type: 'Fetch Error',
                        status: response.status,
                        statusText: response.statusText,
                        url: args[0],
                        timestamp: new Date().toISOString()
                    };
                    
                    console.error('❌ Fetch Failed:', error);
                    logErrorToServer(error);
                }
                return response;
            })
            .catch(err => {
                const error = {
                    type: 'Fetch Exception',
                    message: err.message,
                    url: args[0],
                    stack: err.stack,
                    timestamp: new Date().toISOString()
                };
                
                console.error('❌ Fetch Exception:', error);
                logErrorToServer(error);
                throw err;
            });
    };

    // 5. JQUERY AJAX ERROR TRACKING
    $(document).ajaxError(function(event, jqXHR, settings, thrownError) {
        const error = {
            type: 'AJAX Error',
            url: settings.url,
            method: settings.type,
            status: jqXHR.status,
            statusText: jqXHR.statusText,
            responseText: jqXHR.responseText ? jqXHR.responseText.substring(0, 500) : '',
            error: thrownError,
            timestamp: new Date().toISOString()
        };
        
        errors.push(error);
        
        console.error('❌ AJAX Error Detected:', error);
        console.error('Response:', jqXHR.responseText);
        
        logErrorToServer(error);
    });

    // 6. LOG PAGE LOAD INFORMATION
    window.addEventListener('DOMContentLoaded', function() {
        console.log('✅ Page Loaded:', {
            url: window.location.href,
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            viewport: {
                width: window.innerWidth,
                height: window.innerHeight
            }
        });

        // Check for images
        const images = document.querySelectorAll('img');
        console.log(`🖼️ Found ${images.length} images on page`);
        
        images.forEach((img, index) => {
            if (!img.complete || img.naturalWidth === 0) {
                console.warn(`⚠️ Image ${index + 1} may have loading issues:`, {
                    src: img.src,
                    alt: img.alt,
                    complete: img.complete,
                    naturalWidth: img.naturalWidth
                });
            }
        });
    });

    // 7. SEND ERROR TO SERVER
    function logErrorToServer(error) {
        try {
            // Use fetch to send error to server
            fetch('/Error/LogClientError', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(error)
            }).catch(err => {
                console.error('Failed to send error to server:', err);
            });
        } catch (e) {
            console.error('Error sending error to server:', e);
        }
    }

    // 8. EXPOSE ERROR LOG FOR DEBUGGING
    window.getErrorLog = function() {
        console.table(errors);
        return errors;
    };

    // 9. PERFORMANCE MONITORING
    window.addEventListener('load', function() {
        setTimeout(function() {
            if (window.performance && window.performance.timing) {
                const perfData = window.performance.timing;
                const pageLoadTime = perfData.loadEventEnd - perfData.navigationStart;
                const connectTime = perfData.responseEnd - perfData.requestStart;
                const renderTime = perfData.domComplete - perfData.domLoading;

                console.log('⚡ Performance Metrics:', {
                    pageLoadTime: pageLoadTime + 'ms',
                    connectTime: connectTime + 'ms',
                    renderTime: renderTime + 'ms'
                });

                if (pageLoadTime > 5000) {
                    console.warn('⚠️ Slow page load detected!');
                }
            }
        }, 0);
    });

    console.log('✅ Error Tracker Ready - Type window.getErrorLog() to see all errors');

})();
