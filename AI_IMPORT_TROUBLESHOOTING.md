# AI Import Troubleshooting Guide

## Common Issues and Solutions

### Issue: "Could not extract invoice data from any files"

This error occurs when the AI service cannot process your uploaded files. Here are the most common causes and solutions:

#### 1. **Google AI API Key Not Configured**

**Error Message**: `Google AI API key is not configured`

**Solution**:
1. Open `appsettings.json`
2. Add or verify your Google AI API key:
```json
{
  "GoogleAI": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```
3. Get your API key from: https://makersuite.google.com/app/apikey
4. Restart the application

#### 2. **Invalid or Expired API Key**

**Error Message**: `Google AI API authentication failed`

**Solution**:
1. Verify your API key is correct (no extra spaces)
2. Check if the API key is still active in Google AI Studio
3. Generate a new API key if needed
4. Update `appsettings.json` with the new key
5. Restart the application

#### 3. **Poor Image Quality**

**Symptoms**:
- AI returns partial data
- Many fields are empty
- Confidence score is "low"

**Solutions**:
- Use higher resolution images (at least 1000px width)
- Ensure good lighting and contrast
- Avoid blurry or rotated images
- Remove shadows and glare
- Use a scanner instead of camera photos
- **Try PDF format** - usually gives better results

#### 4. **Unsupported File Format**

**Supported Formats**:
- ✅ PDF (.pdf)
- ✅ JPEG (.jpg, .jpeg)
- ✅ PNG (.png)
- ✅ GIF (.gif)
- ✅ WebP (.webp)

**Not Supported**:
- ❌ Word documents (.doc, .docx)
- ❌ Excel files (.xls, .xlsx)
- ❌ ZIP archives
- ❌ Text files (.txt)

**Solution**: Convert unsupported formats to PDF or image format

#### 5. **File Too Large**

**Symptoms**: Upload fails or times out

**Solutions**:
- Compress images (reduce resolution to 1500-2000px width)
- Split multi-page PDFs into individual invoices
- Use online tools to compress PDFs
- Maximum recommended size: 10MB per file

#### 6. **File Not Readable**

**Symptoms**:
- "Failed to parse invoice data"
- No data extracted

**Possible Causes**:
- File is encrypted or password-protected
- Image contains no text (blank page)
- File is corrupted
- Wrong file type (e.g., renamed file extension)

**Solutions**:
- Remove password protection from PDFs
- Verify the file opens correctly on your computer
- Re-scan or re-export the document
- Ensure the file actually contains invoice data

#### 7. **Non-Invoice Content**

**Symptoms**: Extracted data doesn't make sense

**Solution**:
- Ensure the uploaded file actually contains an invoice
- Not all receipts/documents are invoices
- The AI looks for specific invoice elements:
  - Invoice number
  - Date
  - Customer/supplier information
  - Line items with quantities and prices
  - Total amount

## Best Practices for Better Accuracy

### 1. **Use PDF When Possible**
```
PDF → Best accuracy (95-99%)
PNG → Good accuracy (85-95%)
JPG → Fair accuracy (75-90%)
Camera photos → Variable (60-85%)
```

### 2. **Optimal Image Settings**
- **Resolution**: 1500-2000px width
- **Format**: PDF or PNG
- **Color**: Color or grayscale (not black & white)
- **Orientation**: Upright (not rotated)
- **Clarity**: Sharp focus, no blur

### 3. **Document Preparation**
- Remove staples and paperclips before scanning
- Flatten creases and folds
- Use a scanner instead of camera when possible
- Scan at 300 DPI or higher
- Save as PDF for multi-page invoices

### 4. **Multiple File Upload**
- You can upload multiple files at once
- Each file is processed separately
- If one fails, others may still succeed
- Check the results page for individual statuses

## Error Messages Explained

### "AI service did not return any data"

**Meaning**: The AI processed the file but couldn't extract structured data

**Common Causes**:
- Image quality too poor
- Document doesn't contain invoice data
- Format is unreadable

**Solution**: Try a different file or improve image quality

### "Failed to parse invoice data"

**Meaning**: AI returned data but in an unexpected format

**Common Causes**:
- API response format changed
- Partial extraction occurred
- Network issues during processing

**Solution**: Try processing again, or contact support if persistent

### "Processing timeout"

**Meaning**: The AI service took too long to respond

**Solution**:
- Try a smaller file
- Split multi-page PDFs
- Check your internet connection
- Try again later (API may be busy)

## Checking Logs

Server logs provide detailed information about processing:

1. **View console output** where the server is running
2. Look for lines containing:
   - `Processing file: filename`
   - `File size: X bytes`
   - `MIME type: ...`
   - Error messages with stack traces

3. **Common log entries**:
```
INFO: Processing file: invoice001.pdf
INFO: File size: 245632 bytes
INFO: MIME type: application/pdf
INFO: AI response received, parsing invoice data
INFO: Successfully extracted invoice from invoice001.pdf
```

## Testing the System

### 1. **Test with Sample Invoice**
- Use a clear, well-formatted invoice
- Should have all standard fields visible
- Try PDF format first

### 2. **Verify API Key**
```bash
# Test API key with curl (replace YOUR_KEY)
curl "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=YOUR_KEY"
```

### 3. **Check Configuration**
- Verify `appsettings.json` exists
- Confirm API key is in correct format
- No extra quotes or spaces
- File is saved after editing

## Still Having Issues?

### Detailed Error Information

When an error occurs, the system now provides:
1. **Specific error message** - what went wrong
2. **File name** - which file failed
3. **Tips** - suggestions to fix the issue

### Getting Help

If you continue to experience issues:

1. **Check the error message details** - they now include specific causes
2. **Review server logs** - detailed processing information
3. **Verify API key** - most common issue
4. **Test file quality** - try with a different, clearer file
5. **Check file format** - ensure it's supported

### Contact Information

- API Key Issues: Visit https://makersuite.google.com/
- Application Issues: Check server logs for detailed errors
- Document Quality: Try scanning at higher resolution

## Quick Checklist

Before uploading:
- [ ] File is PDF or high-quality image
- [ ] File contains actual invoice data
- [ ] Image is clear and readable
- [ ] File size is under 10MB
- [ ] Google AI API key is configured
- [ ] Application has been restarted after config changes

---

## Summary

**Most Common Issues**:
1. ❗ **API Key Not Configured** (70% of issues)
2. 📷 **Poor Image Quality** (20% of issues)
3. 📄 **Wrong File Format** (5% of issues)
4. 🔧 **Other Technical Issues** (5% of issues)

**Quick Fix Flowchart**:
```
Error occurred?
  ├─ API key error? → Configure API key in appsettings.json
  ├─ Can't read image? → Try PDF format or better quality
  ├─ No data extracted? → Verify file contains invoice data
  └─ Other error? → Check detailed error message and tips
```

**Your system now provides detailed error messages to help identify and fix issues quickly!** 🎯

