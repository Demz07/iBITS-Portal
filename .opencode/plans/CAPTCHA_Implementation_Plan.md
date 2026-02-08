# CAPTCHA Implementation Plan for iBITS Portal Login System
# Date: 2025-02-05
# Status: FOR FUTURE IMPLEMENTATION (after critical fixes are complete)

## CAPTCHA Integration Overview
This plan details how to add Google reCAPTCHA v2 protection to the login page after repeated failed attempts.

## Implementation Requirements

### Prerequisites
- Google reCAPTCHA v2 API keys (Site Key & Secret Key)
- Google reCAPTCHA Admin Console setup for your domain

### Files to Create/Modify

#### 1. CAPTCHA Configuration (appsettings.json)
```json
{
  "Recaptcha": {
    "SiteKey": "your-site-key-here",
    "SecretKey": "your-secret-key-here"
  }
}
```

#### 2. Create RecaptchaService (Services/RecaptchaService.cs)
- Service class for validating reCAPTCHA responses
- HTTP client to communicate with Google reCAPTCHA API
- JSON models for request/response handling

#### 3. Update Login.cshtml
- Add reCAPTCHA widget (conditionally show after 3 failed attempts)
- Include Google reCAPTCHA script
- Add reCAPTCHA validation to form

#### 4. Update Login.cshtml.cs
- Add failed attempt counter tracking
- Show CAPTCHA after 3 failed attempts
- Validate CAPTCHA response during login
- Reset attempt counter on successful login

## Implementation Steps

### Step 1: Configuration Setup
1. Get Google reCAPTCHA v2 keys from Google reCAPTCHA Console
2. Add keys to appsettings.json
3. Configure reCAPTCHA for your domain (localhost:7010 for development)

### Step 2: Backend Implementation
1. Create RecaptchaService class
2. Add reCAPTCHA validation method
3. Add attempt tracking to Login.cshtml.cs
4. Integrate CAPTCHA validation in login logic

### Step 3: Frontend Integration
1. Add reCAPTCHA widget to Login.cshtml
2. Show/hide based on failed attempt count
3. Add client-side validation
4. Style to match existing design

### Step 4: Testing & Validation
1. Test CAPTCHA appears after 3 failed attempts
2. Test CAPTCHA validation works correctly
3. Test successful login resets counter
4. Test CAPTCHA works with different user roles

## Code Implementation Details

### RecaptchaService.cs Structure
```csharp
public class RecaptchaService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    
    public async Task<bool> ValidateRecaptchaAsync(string token)
    {
        // Validate reCAPTCHA token with Google API
        // Return validation result
    }
}
```

### Login.cshtml.cs Modifications
```csharp
// Track failed attempts in TempData/session
private const int MaxAttemptsBeforeCaptcha = 3;

// In OnGetAsync:
var failedAttempts = TempData.GetInt32("FailedAttempts") ?? 0;
ShowCaptcha = failedAttempts >= MaxAttemptsBeforeCaptcha;

// In OnPostAsync:
if (ShowCaptcha)
{
    var recaptchaResult = await _recaptchaService.ValidateRecaptchaAsync(Input.RecaptchaToken);
    if (!recaptchaResult)
    {
        ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA verification.");
        return Page();
    }
}
```

### Login.cshtml Modifications
```html
@if (Model.ShowCaptcha)
{
    <div class="form-group">
        <div class="g-recaptcha" data-sitekey="@Model.RecaptchaSiteKey"></div>
        <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    </div>
}
```

## Security Considerations
- Store reCAPTCHA keys securely in configuration
- Validate CAPTCHA token server-side
- Implement rate limiting for CAPTCHA validation requests
- Log CAPTCHA validation attempts for monitoring

## Testing Checklist
- [ ] CAPTCHA appears after 3 failed attempts
- [ ] CAPTCHA validation prevents login when invalid
- [ ] CAPTCHA resets on successful login
- [ ] CAPTCHA works with different user types (Admin/Member)
- [ ] Mobile responsiveness
- [ ] Accessibility compliance
- [ ] Error messages are user-friendly

## Deployment Notes
- Add reCAPTCHA keys to production configuration
- Update Google reCAPTCHA console with production domain
- Test CAPTCHA in production environment
- Monitor CAPTCHA performance and success rates

## Future Enhancements
- Consider reCAPTCHA v3 for frictionless validation
- Implement CAPTCHA for other sensitive actions (password reset, etc.)
- Add analytics for CAPTCHA performance
- Consider alternative CAPTCHA services if needed

## Implementation Time Estimate
- **Total Time: 30-40 minutes**
- **Backend Service: 15-20 minutes**
- **Frontend Integration: 10-15 minutes**
- **Testing & Validation: 5-10 minutes**

## Dependencies
- Google reCAPTCHA v2 API
- HttpClient for API calls
- Newtonsoft.Json or System.Text.Json
- ASP.NET Core Configuration

---
**Note**: Implement this only after basic lockout protection is working properly and the crash issue is resolved.