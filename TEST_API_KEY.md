# Testing Google AI API Key

## Your Current Configuration

**API Key**: `AIzaSyCSVhO-6baKAiExRQCsX6HCDIKsHB550Fg`

## Quick Test

Run this command to test if your API key works:

```bash
curl -X POST \
  "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=AIzaSyCSVhO-6baKAiExRQCsX6HCDIKsHB550Fg" \
  -H "Content-Type: application/json" \
  -d '{
    "contents": [{
      "parts": [{
        "text": "Say hello"
      }]
    }]
  }'
```

## Expected Response

If the API key works, you should see a JSON response with generated content.

If it doesn't work, you'll see an error like:
- `401 Unauthorized` - API key is invalid
- `403 Forbidden` - API key doesn't have permission
- `429 Too Many Requests` - Rate limit exceeded

## Possible Issues

### 1. **API Key Invalid or Expired**
- Go to https://makersuite.google.com/app/apikey
- Check if your key is still active
- Generate a new key if needed

### 2. **API Not Enabled**
- Visit https://console.cloud.google.com/
- Enable the "Generative Language API"
- Make sure the API is enabled for your project

### 3. **Quota Exceeded**
- Check your usage at https://console.cloud.google.com/
- You may have hit the free tier limit
- Wait for quota reset or upgrade

### 4. **Image Processing Limitations**

The Gemini API has some limitations:
- Maximum file size: ~4MB for images
- Supported formats: JPEG, PNG, GIF, WebP
- Image must be readable and clear
- Complex or low-quality images may fail

## Recommended Actions

1. **Test the API key** with the curl command above
2. **Try with a different image**:
   - Use a clear, high-quality image
   - Try a PDF instead of JPEG
   - Use a simple, well-formatted invoice

3. **Check API Status**:
   - Visit https://status.cloud.google.com/
   - Verify Google AI services are operational

4. **Generate New Key** (if needed):
   - Go to https://makersuite.google.com/app/apikey
   - Create a new API key
   - Update appsettings.json
   - Restart the application

## Alternative: Try Manual Test

1. Go to https://makersuite.google.com/
2. Try uploading your invoice image there
3. Ask Gemini to extract invoice data
4. If it works there, the issue is with our implementation
5. If it doesn't work there, the issue is with the image or API

---

**Based on the error, the most likely issue is either:**
- The image quality is too poor for the AI to process
- The API key needs to be regenerated
- There are API usage limits being hit

**Next Steps**: Try the curl command above to verify the API key works!

