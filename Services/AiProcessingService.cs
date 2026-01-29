using System.Text;
using System.Text.Json;
using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    /// <summary>
    /// AI-Powered Document Processing Service
    /// Uses OpenAI GPT-5.2 for advanced document analysis and data extraction
    /// Features: Advanced OCR, intelligent data extraction, contextual understanding, multi-format support
    /// Configuration priority: 1) Database SystemSettings, 2) appsettings.json, 3) Environment variable
    /// </summary>
    public class AiProcessingService : IAiProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private string? _apiKey;

        public AiProcessingService(HttpClient httpClient, IConfiguration configuration, ILogger<AiProcessingService> logger, IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Initialize API key (will be refreshed from database on each request)
            InitializeApiKey();
        }

        private void InitializeApiKey()
        {
            // Try to get API key from configuration first
            _apiKey = _configuration["OpenAI:ApiKey"];

            // If not found in config, check environment variable (for production deployment)
            if (string.IsNullOrEmpty(_apiKey))
            {
                _apiKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey")
                       ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation("OpenAI API key configured from config/environment");
            }
        }

        private async Task<string?> GetApiKeyAsync()
        {
            // First, try to get from database (highest priority for production)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var setting = await dbContext.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "OpenAIApiKey");

                if (setting != null && !string.IsNullOrEmpty(setting.SettingValue))
                {
                    _logger.LogInformation("OpenAI API key loaded from database settings (key length: {Length})", setting.SettingValue.Length);
                    return setting.SettingValue;
                }
                else
                {
                    _logger.LogWarning("OpenAIApiKey setting found but value is empty or null. Setting exists: {Exists}", setting != null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load API key from database, falling back to config/environment");
            }

            // Fall back to config/environment variable
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation("Using API key from config/environment variable");
                return _apiKey;
            }

            _logger.LogError("No OpenAI API key found in database, config, or environment variables");
            return null;
        }

        public async Task<Invoice?> ExtractInvoiceFromFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Get API key (checks database first, then config/environment)
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("OpenAI API key is not configured. Please set it in Admin Portal > System Settings, or add OpenAI:ApiKey to appsettings.json");
                    throw new InvalidOperationException("OpenAI API key is not configured. Set it in Admin Portal > System Settings (OpenAIApiKey) or in appsettings.json");
                }

                _logger.LogInformation($"Processing file: {fileName}");

                // Convert file to base64
                var fileBytes = await ReadStreamAsBytesAsync(fileStream);
                var fileSizeMB = fileBytes.Length / 1024.0 / 1024.0;
                _logger.LogInformation($"File size: {fileBytes.Length} bytes ({fileSizeMB:F2} MB)");

                // Check file size limits (7 MB for inline_data as per Google documentation)
                if (fileSizeMB > 7)
                {
                    throw new Exception($"File {fileName} is too large ({fileSizeMB:F2} MB). Maximum file size is 7 MB for inline processing. Please use a smaller file or compress the PDF.");
                }

                var base64File = Convert.ToBase64String(fileBytes);
                var mimeType = GetMimeType(fileName);
                _logger.LogInformation($"MIME type: {mimeType}");

                // Enhanced prompt for Gemini 2.5 Pro - leveraging advanced document analysis
                var prompt = @"You are an expert AI document analyst powered by Gemini 2.5 Pro with advanced OCR and document understanding capabilities. Perform a comprehensive deep analysis of this SUPPLIER INVOICE document (a bill from a supplier to a company).

IMPORTANT CONTEXT: This is a SUPPLIER INVOICE (Accounts Payable) - an invoice FROM a supplier TO a company. Extract the SUPPLIER information (the entity billing/sending the invoice), NOT the buyer/recipient company.

**CRITICAL: SCAN ALL PAGES OF THIS DOCUMENT**
- This document may have MULTIPLE PAGES
- You MUST read and analyze EVERY page of this PDF
- Extract information from ALL pages, not just the first page
- Line items may span multiple pages
- Totals are often on the LAST page
- If there are MULTIPLE SEPARATE INVOICES in this document, extract the FIRST invoice and include a warning noting how many other invoices were found

ADVANCED SCANNING PROTOCOL:

1. MULTI-PAGE DOCUMENT ANALYSIS:
   - Scan ALL pages from beginning to end
   - Track page continuations (e.g., 'Page 1 of 3')
   - Identify continued line items across pages
   - Look for summary/total on the last page
   - Combine data from all related pages

2. DOCUMENT STRUCTURE ANALYSIS:
   - Identify document layout, headers, footers, and sections
   - Detect tables, columns, and data relationships
   - Recognize logos, watermarks, stamps, and signatures
   - Read both printed text and handwritten annotations
   - Process multi-column layouts and complex formatting

3. INTELLIGENT DATA EXTRACTION:
   - Extract SUPPLIER/VENDOR information (the company sending the invoice, usually at the top)
   - Distinguish between 'Bill From' (supplier) and 'Bill To' (buyer/recipient)
   - Use contextual understanding to identify invoice elements
   - Cross-reference multiple data points for accuracy
   - Detect and handle multiple date formats automatically
   - Identify currency symbols and decimal separators
   - Extract data from tables with merged cells or irregular layouts

4. QUALITY ASSURANCE:
   - Verify mathematical accuracy of all calculations
   - Cross-check totals against line item sums
   - Validate date logic (invoice date should be before or equal to due date)
   - Identify missing or ambiguous information
   - Flag inconsistencies or potential errors

COMPREHENSIVE EXTRACTION REQUIREMENTS:
- Invoice Number: Detect all variations (Invoice #, Inv No., Bill #, Reference, etc.)
- Dates: Parse ANY date format and convert to YYYY-MM-DD (handle DD/MM/YYYY, MM-DD-YY, written dates, etc.)
- SUPPLIER Information (Bill From): Extract SUPPLIER/VENDOR details including:
  * Supplier name (company name at top/header of invoice)
  * Supplier address (full address)
  * Supplier email
  * Supplier phone
  * Tax ID/VAT number
- Line Items: Extract ALL items from ALL PAGES including hidden details, notes, SKU/part numbers
- Pricing: Capture unit prices, quantities, discounts, taxes, subtotals
- Payment Terms: Extract payment methods, bank details, payment instructions
- Additional Notes: Capture terms, conditions, special instructions, delivery notes

Return ONLY a valid JSON object with this EXACT structure (no markdown, no extra text):
{
  ""invoiceNumber"": ""string (required)"",
  ""invoiceDate"": ""YYYY-MM-DD (required)"",
  ""dueDate"": ""YYYY-MM-DD (required)"",
  ""customerName"": ""SUPPLIER/VENDOR company name (required - the entity billing you)"",
  ""customerAddress"": ""SUPPLIER/VENDOR full address or null"",
  ""customerEmail"": ""SUPPLIER/VENDOR email or null"",
  ""customerPhone"": ""SUPPLIER/VENDOR phone or null"",
  ""totalAmount"": 0.00,
  ""notes"": ""string or null"",
  ""items"": [
    {
      ""description"": ""string (detailed description)"",
      ""quantity"": 1,
      ""unitPrice"": 0.00
    }
  ],
  ""pagesScanned"": 1,
  ""multipleInvoicesDetected"": false,
  ""totalInvoicesInDocument"": 1,
  ""confidence"": ""high"",
  ""warnings"": null
}

VALIDATION RULES:
- All required fields must have values
- Dates must be valid and logical (invoice date should come before or equal to due date)
- All amounts must be positive numbers
- Quantity must be at least 1
- Total should match sum of (quantity times unitPrice) for all items
- If total does not match, note it in warnings
- customerName should contain the SUPPLIER name (who is billing), not the buyer
- If multiple invoices are detected, set multipleInvoicesDetected to true and include a warning

Return ONLY the JSON object, nothing else.";

                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType, apiKey);

                if (response == null)
                {
                    _logger.LogWarning($"No response from AI for file: {fileName}");
                    throw new Exception($"AI service did not return any data for {fileName}. This could be due to: image quality issues, unsupported format, or API limitations.");
                }

                _logger.LogInformation($"AI response received, parsing invoice data from {fileName}");
                var invoice = ParseInvoiceFromJson(response);

                if (invoice == null)
                {
                    throw new Exception($"Failed to parse invoice data from {fileName}. AI response format was invalid.");
                }

                return invoice;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw configuration errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting invoice from file: {fileName}");
                throw new Exception($"Error processing {fileName}: {ex.Message}", ex);
            }
        }

        public async Task<Payment?> ExtractPaymentFromFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Get API key (checks database first, then config/environment)
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("OpenAI API key is not configured");
                    return null;
                }

                var fileBytes = await ReadStreamAsBytesAsync(fileStream);
                var base64File = Convert.ToBase64String(fileBytes);
                var mimeType = GetMimeType(fileName);

                // ENHANCED Gemini 2.5 Pro Prompt - Specialized for PNG Bank Statements (BSP, Westpac, ANZ, Kina Bank)
                var prompt = @"You are an elite financial AI analyst specialized in Papua New Guinea (PNG) banking documents, particularly BSP (Bank South Pacific) INTERNAL PAY NOW receipts and bank statements.

═══════════════════════════════════════════════════════════════════════════════
                    PNG BANK STATEMENT ANALYSIS PROTOCOL
                    (BSP, Westpac PNG, ANZ PNG, Kina Bank)
═══════════════════════════════════════════════════════════════════════════════

🔍 PHASE 1: DOCUMENT RECOGNITION
──────────────────────────────────────────────────────────────────────────────
Identify document type:
• BSP INTERNAL PAY NOW - Internal transfer within BSP bank
• BSP DOMESTIC PAYMENT - Transfer to other PNG banks
• WESTPAC PNG TRANSFER - Westpac bank transfer
• ANZ PNG PAYMENT - ANZ bank payment
• KINA BANK TRANSFER - Kina Bank transfer
• GENERAL BANK STATEMENT - Other bank documents

Look for bank logos and headers:
• BSP (Bank South Pacific) - Green logo
• Westpac PNG - Red 'W' logo
• ANZ PNG - Blue logo
• Kina Bank - Orange/Yellow logo

🔍 PHASE 2: BSP INTERNAL PAY NOW SPECIFIC FIELDS
──────────────────────────────────────────────────────────────────────────────

HEADER INFORMATION:
• Company Name (appears at top, e.g., ""APCM BALIMO HOSPITAL"") - This is the PAYER/OUR COMPANY
• Transaction Date/Time (top right, e.g., ""02 Oct 2025 03:55:50 PM"")

FIELD EXTRACTION (Read these EXACT labels):

1. REFERENCE NUMBER:
   - Transaction identifier (e.g., ""2527501057343002"")
   - This uniquely identifies the bank transaction

2. TRANSFER TO:
   - Supplier name WITH bank suffix (e.g., ""Roselaw Limited BSP"")
   - The ""BSP"" at the end means supplier banks with BSP
   - Other suffixes: ""Westpac"", ""ANZ"", ""Kina""

3. ACCOUNT TYPE:
   - ""Internal"" = BSP to BSP transfer (same bank)
   - ""Domestic"" = BSP to other PNG bank (different bank)

4. ACCOUNT NUMBER:
   - Full supplier account (e.g., ""3447021471979"")
   - PARSE AS: First 3 digits = Branch Number (e.g., ""344"")
   - PARSE AS: Remaining digits = Account Number (e.g., ""7021471979"")

5. ACCOUNT NAME:
   - Supplier/Payee name WITHOUT bank suffix (e.g., ""Roselaw Limited"")
   - This is the clean supplier name for matching

6. TRANSFER FROM:
   - Our company's bank account (e.g., ""9501000585602"")
   - PARSE AS: First 3 digits = Our Branch (e.g., ""950"")
   - PARSE AS: Remaining digits = Our Account (e.g., ""1000585602"")

7. AMOUNT:
   - Payment amount with currency (e.g., ""PGK 13,508.88"")
   - Extract numeric value: 13508.88
   - Currency is typically PGK (Papua New Guinea Kina)

8. TRANSFER WHEN:
   - Payment date in format ""DD Mon YYYY"" (e.g., ""02 Oct 2025"")
   - Convert to YYYY-MM-DD format for output

9. PURPOSE:
   - Payment reason (often empty)
   - May contain invoice reference or description

10. NOTE:
    - Comments/reference field
    - Often contains invoice number (e.g., ""InvNo1956_VehicleFreight_RoselawLtd"")
    - Extract invoice number if present

🔍 PHASE 3: INTELLIGENT PARSING
──────────────────────────────────────────────────────────────────────────────

BANK NAME EXTRACTION from Transfer To:
• If ends with "" BSP"" → Bank = ""BSP"" (Bank South Pacific)
• If ends with "" Westpac"" → Bank = ""Westpac""
• If ends with "" ANZ"" → Bank = ""ANZ""
• If ends with "" Kina"" → Bank = ""Kina Bank""

ACCOUNT NUMBER PARSING:
For PNG bank accounts, the format is typically:
• 3-digit branch code + 10-digit account number = 13 digits total
• Example: 3447021471979 → Branch: 344, Account: 7021471979

INVOICE REFERENCE EXTRACTION from Note field:
• Look for patterns: ""InvNo"", ""INV"", ""Invoice"", ""Inv #""
• Example: ""InvNo1956_VehicleFreight_RoselawLtd"" → Invoice: ""1956""

══════════════════════════════════════════════════════════════════════════════
                              OUTPUT REQUIREMENTS
══════════════════════════════════════════════════════════════════════════════

Return ONLY a valid JSON object with this EXACT structure. NO markdown, NO code blocks:

{
  ""documentType"": ""BSP_INTERNAL_PAY_NOW or BSP_DOMESTIC_PAYMENT or BANK_TRANSFER"",
  ""referenceNumber"": ""string - Reference Number from statement (REQUIRED)"",
  ""paymentDate"": ""YYYY-MM-DD - Transfer When date converted (REQUIRED)"",
  ""amount"": 0.00,
  ""currency"": ""PGK"",
  ""paymentMethod"": ""Bank Transfer"",
  ""transferTo"": ""string - Full Transfer To field including bank suffix"",
  ""accountType"": ""Internal or Domestic"",
  ""payeeAccountFull"": ""string - Full Account Number field"",
  ""payeeBranchNumber"": ""string - First 3 digits of Account Number"",
  ""payeeAccountNumber"": ""string - Remaining digits after branch"",
  ""payeeName"": ""string - Account Name (supplier without bank suffix)"",
  ""payeeBankName"": ""string - Bank name extracted from Transfer To suffix (BSP, Westpac, etc.)"",
  ""payerAccountFull"": ""string - Full Transfer From field"",
  ""payerBranchNumber"": ""string - First 3 digits of Transfer From"",
  ""payerAccountNumber"": ""string - Remaining digits of Transfer From"",
  ""payerName"": ""string - Company name from header (Our organization)"",
  ""purpose"": ""string or null - Purpose field content"",
  ""notes"": ""string or null - Note field content"",
  ""relatedInvoiceNumber"": ""string or null - Invoice number if found in Note"",
  ""confidence"": ""high/medium/low"",
  ""extractionNotes"": ""string - Any observations about the extraction""
}

CRITICAL RULES:
✓ Return ONLY the JSON object - no markdown code blocks
✓ Reference Number and Payment Date are REQUIRED
✓ Date MUST be converted to YYYY-MM-DD format
✓ Amount MUST be numeric (no currency symbol, no commas)
✓ Parse account numbers into branch + account components
✓ Extract bank name from Transfer To suffix
✓ Look for invoice references in Note field";

                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType, apiKey);

                if (response == null)
                    return null;

                return ParseEnhancedPaymentFromJson(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting payment from file");
                return null;
            }
        }

        /// <summary>
        /// Parse the enhanced payment JSON response from Gemini (supports BSP PNG bank statements)
        /// </summary>
        private Payment? ParseEnhancedPaymentFromJson(string jsonResponse)
        {
            try
            {
                // Clean up the response - remove any markdown code blocks
                var cleanJson = jsonResponse.Trim();
                if (cleanJson.StartsWith("```json"))
                    cleanJson = cleanJson.Substring(7);
                else if (cleanJson.StartsWith("```"))
                    cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```"))
                    cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                // Generate payment number from reference or timestamp
                var refNumber = GetJsonString(root, "referenceNumber");
                var paymentNumber = !string.IsNullOrEmpty(refNumber)
                    ? $"PAY-{refNumber.Substring(Math.Max(0, refNumber.Length - 8))}"
                    : $"PAY-{DateTime.Now:yyyyMMddHHmmss}";

                var payment = new Payment
                {
                    // Core payment fields
                    PaymentNumber = paymentNumber,
                    PaymentDate = ParseJsonDate(root, "paymentDate") ?? DateTime.Now,
                    Amount = GetJsonDecimal(root, "amount") ?? 0,
                    PaymentMethod = NormalizePaymentMethod(GetJsonString(root, "paymentMethod")) ?? "Bank Transfer",
                    ReferenceNumber = refNumber,
                    Currency = GetJsonString(root, "currency") ?? "PGK",
                    Status = "Unallocated",

                    // BSP-specific fields
                    TransferTo = GetJsonString(root, "transferTo"),
                    AccountType = GetJsonString(root, "accountType"),

                    // Payee (Supplier) bank details
                    BankAccountNumber = GetJsonString(root, "payeeAccountFull"),
                    PayeeBranchNumber = GetJsonString(root, "payeeBranchNumber"),
                    PayeeAccountNumber = GetJsonString(root, "payeeAccountNumber"),
                    PayeeName = GetJsonString(root, "payeeName"),
                    BankName = GetJsonString(root, "payeeBankName"),

                    // Payer (Our Company) bank details
                    PayerAccountNumber = GetJsonString(root, "payerAccountFull"),
                    PayerBranchNumber = GetJsonString(root, "payerBranchNumber"),
                    PayerBankAccountNumber = GetJsonString(root, "payerAccountNumber"),
                    PayerName = GetJsonString(root, "payerName"),

                    // Purpose and Notes
                    Purpose = GetJsonString(root, "purpose"),
                    Notes = BuildPaymentNotes(root)
                };

                // Log extraction details
                var confidence = GetJsonString(root, "confidence") ?? "unknown";
                var docType = GetJsonString(root, "documentType") ?? "unknown";
                _logger.LogInformation($"BSP Payment extracted - Type: {docType}, Ref: {payment.ReferenceNumber}, Amount: {payment.Currency} {payment.Amount:N2}, Payee: {payment.PayeeName}, Confidence: {confidence}");

                if (payment.Amount <= 0)
                {
                    _logger.LogWarning("Extracted payment has zero or negative amount");
                    return null;
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing enhanced payment JSON: {Response}", jsonResponse);
                return null;
            }
        }

        /// <summary>
        /// Build comprehensive notes from extracted payment data
        /// </summary>
        private string BuildPaymentNotes(JsonElement root)
        {
            var notes = new StringBuilder();

            var docType = GetJsonString(root, "documentType");
            if (!string.IsNullOrEmpty(docType))
                notes.AppendLine($"Document Type: {docType}");

            var relatedInvoice = GetJsonString(root, "relatedInvoiceNumber");
            if (!string.IsNullOrEmpty(relatedInvoice))
                notes.AppendLine($"Related Invoice: {relatedInvoice}");

            var extractionNotes = GetJsonString(root, "extractionNotes");
            if (!string.IsNullOrEmpty(extractionNotes))
                notes.AppendLine($"AI Notes: {extractionNotes}");

            var originalNotes = GetJsonString(root, "notes");
            if (!string.IsNullOrEmpty(originalNotes))
                notes.AppendLine($"Note: {originalNotes}");

            return notes.ToString().Trim();
        }

        /// <summary>
        /// Normalize payment method to standard values
        /// </summary>
        private string NormalizePaymentMethod(string? method)
        {
            if (string.IsNullOrEmpty(method))
                return "Bank Transfer";

            var normalized = method.ToLowerInvariant().Trim();

            return normalized switch
            {
                "cash" => "Cash",
                "check" or "cheque" or "bank draft" => "Check",
                "credit card" or "creditcard" or "visa" or "mastercard" or "amex" or "american express" => "Credit Card",
                "debit card" or "debitcard" or "eftpos" => "Debit Card",
                "bank transfer" or "wire" or "wire transfer" or "ach" or "eft" or "direct deposit" or "electronic transfer" => "Bank Transfer",
                "paypal" => "PayPal",
                "mobile payment" or "apple pay" or "google pay" or "samsung pay" => "Mobile Payment",
                "direct debit" or "autopay" or "auto-pay" => "Direct Debit",
                "cryptocurrency" or "bitcoin" or "btc" or "eth" or "crypto" => "Cryptocurrency",
                "money order" or "postal order" => "Money Order",
                _ => "Other"
            };
        }

        public async Task<List<Invoice>> ProcessInvoiceBatchAsync(List<(Stream stream, string fileName)> files)
        {
            var invoices = new List<Invoice>();

            foreach (var (stream, fileName) in files)
            {
                var invoice = await ExtractInvoiceFromFileAsync(stream, fileName);
                if (invoice != null)
                {
                    invoices.Add(invoice);
                }
            }

            return invoices;
        }

        public async Task<List<Payment>> ProcessPaymentBatchAsync(List<(Stream stream, string fileName)> files)
        {
            var payments = new List<Payment>();

            foreach (var (stream, fileName) in files)
            {
                var payment = await ExtractPaymentFromFileAsync(stream, fileName);
                if (payment != null)
                {
                    payments.Add(payment);
                }
            }

            return payments;
        }

        public async Task<string> MatchPaymentToInvoiceAsync(Payment payment, List<Invoice> invoices)
        {
            try
            {
                // Get API key (checks database first, then config/environment)
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return "API key not configured";
                }

                var invoicesJson = JsonSerializer.Serialize(invoices.Select(i => new
                {
                    i.Id,
                    i.InvoiceNumber,
                    i.CustomerName,
                    i.TotalAmount,
                    i.BalanceAmount,
                    i.InvoiceDate
                }));

                var paymentJson = JsonSerializer.Serialize(new
                {
                    payment.PaymentNumber,
                    payment.Amount,
                    payment.PaymentDate,
                    payment.ReferenceNumber,
                    payment.Notes
                });

                var prompt = $@"Given this payment information:
{paymentJson}

And these available invoices:
{invoicesJson}

Determine which invoice this payment should be matched to. Return ONLY a JSON object with this exact structure:
{{
  ""invoiceId"": number or null,
  ""confidence"": ""high/medium/low"",
  ""reason"": ""brief explanation""
}}

Consider: invoice numbers mentioned, amounts, customer names, dates, and reference numbers. Return ONLY the JSON object, nothing else.";

                var response = await CallGeminiTextApiAsync(prompt, apiKey);
                return response ?? "No match found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching payment to invoice");
                return "Error in matching";
            }
        }

        private async Task<string?> CallGeminiVisionApiAsync(string prompt, string base64Image, string mimeType, string apiKey)
        {
            try
            {
                // Using OpenAI GPT-5.2 with vision capability
                var requestBody = new
                {
                    model = "gpt-5.2",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = prompt },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:{mimeType};base64,{base64Image}"
                                    }
                                }
                            }
                        }
                    },
                    max_tokens = 8192,
                    temperature = 0.1
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set the Authorization header for OpenAI API
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var url = "https://api.openai.com/v1/chat/completions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API error: {response.StatusCode} - {errorContent}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new InvalidOperationException($"OpenAI API authentication failed. Please check your API key. Status: {response.StatusCode}");
                    }

                    throw new Exception($"OpenAI API error ({response.StatusCode}): {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"OpenAI API response length: {responseContent.Length} characters");

                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Check if response has error
                if (result.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                    _logger.LogError($"API returned error: {errorMessage}");
                    throw new Exception($"OpenAI API error: {errorMessage}");
                }

                if (result.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];

                    // Check finish reason
                    if (firstChoice.TryGetProperty("finish_reason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        _logger.LogInformation($"API finish reason: {reason}");

                        if (reason != "stop")
                        {
                            _logger.LogWarning($"API stopped with reason: {reason}. Content may be incomplete.");
                            if (reason == "content_filter")
                            {
                                throw new Exception("Content was blocked by safety filters. Please try a different file.");
                            }
                        }
                    }

                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentText))
                    {
                        var text = contentText.GetString();
                        _logger.LogInformation("Successfully extracted text from API response");
                        return CleanJsonResponse(text);
                    }
                }

                // Log if we got a response but couldn't parse it
                _logger.LogWarning($"API returned data but couldn't extract text. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI Vision API");
                throw new Exception($"Failed to call OpenAI API: {ex.Message}", ex);
            }
        }

        private async Task<string?> CallGeminiTextApiAsync(string prompt, string apiKey)
        {
            try
            {
                // Using OpenAI GPT-5.2 for text processing
                var requestBody = new
                {
                    model = "gpt-5.2",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = 8192,
                    temperature = 0.1
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set the Authorization header for OpenAI API
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var url = "https://api.openai.com/v1/chat/completions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentText))
                    {
                        return CleanJsonResponse(contentText.GetString());
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI Text API");
                return null;
            }
        }

        private string CleanJsonResponse(string? response)
        {
            if (string.IsNullOrEmpty(response))
                return "{}";

            // Remove markdown code blocks if present
            response = response.Trim();
            if (response.StartsWith("```json"))
                response = response.Substring(7);
            else if (response.StartsWith("```"))
                response = response.Substring(3);

            if (response.EndsWith("```"))
                response = response.Substring(0, response.Length - 3);

            return response.Trim();
        }

        private Invoice? ParseInvoiceFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<InvoiceDto>(json, options);

                if (data == null)
                {
                    _logger.LogWarning("Failed to deserialize invoice JSON");
                    return null;
                }

                // Validate required fields
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
                    validationErrors.Add("Invoice number is missing");

                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    validationErrors.Add("Customer name is missing");

                if (data.TotalAmount <= 0)
                    validationErrors.Add("Total amount must be greater than zero");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Invoice validation failed: {Errors}", string.Join(", ", validationErrors));
                    return null;
                }

                // Parse and validate dates
                var invoiceDate = ParseDate(data.InvoiceDate);
                var dueDate = ParseDate(data.DueDate);

                if (!invoiceDate.HasValue)
                {
                    _logger.LogWarning("Invalid invoice date, using current date");
                    invoiceDate = DateTime.Now;
                }

                if (!dueDate.HasValue || dueDate < invoiceDate)
                {
                    _logger.LogWarning("Invalid due date, setting to 30 days from invoice date");
                    dueDate = invoiceDate.Value.AddDays(30);
                }

                var invoice = new Invoice
                {
                    InvoiceNumber = data.InvoiceNumber!.Trim(),
                    InvoiceDate = invoiceDate.Value,
                    DueDate = dueDate.Value,
                    CustomerName = data.CustomerName!.Trim(),
                    CustomerAddress = data.CustomerAddress?.Trim(),
                    CustomerEmail = ValidateEmail(data.CustomerEmail),
                    CustomerPhone = data.CustomerPhone?.Trim(),
                    TotalAmount = data.TotalAmount,
                    Notes = BuildNotes(data.Notes, data.Confidence, data.Warnings,
                        data.MultipleInvoicesDetected, data.TotalInvoicesInDocument ?? 1, data.PagesScanned ?? 1),
                    InvoiceType = "Supplier", // Imported/uploaded invoices are from suppliers (AP - Accounts Payable)
                    Status = "Unpaid",
                    PaidAmount = 0,
                    InvoiceItems = new List<InvoiceItem>()
                };

                // Process line items
                if (data.Items != null && data.Items.Any())
                {
                    foreach (var item in data.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.Description))
                            continue;

                        invoice.InvoiceItems.Add(new InvoiceItem
                        {
                            Description = item.Description.Trim(),
                            Quantity = Math.Max(1, item.Quantity),
                            UnitPrice = Math.Max(0, item.UnitPrice)
                        });
                    }
                }

                // If no items but has total, create a single item
                if (!invoice.InvoiceItems.Any() && invoice.TotalAmount > 0)
                {
                    invoice.InvoiceItems.Add(new InvoiceItem
                    {
                        Description = "Service/Product",
                        Quantity = 1,
                        UnitPrice = invoice.TotalAmount
                    });
                }

                // Verify total matches items
                var calculatedTotal = invoice.InvoiceItems.Sum(i => i.Quantity * i.UnitPrice);
                if (Math.Abs(calculatedTotal - invoice.TotalAmount) > 0.01m && calculatedTotal > 0)
                {
                    _logger.LogWarning("Invoice total mismatch: Declared={Declared}, Calculated={Calculated}",
                        invoice.TotalAmount, calculatedTotal);

                    // Use calculated total if items are present
                    invoice.TotalAmount = calculatedTotal;
                }

                _logger.LogInformation("Successfully parsed invoice {InvoiceNumber} with {ItemCount} items, Confidence: {Confidence}",
                    invoice.InvoiceNumber, invoice.InvoiceItems.Count, data.Confidence ?? "unknown");

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing invoice JSON: {Json}", json);
                return null;
            }
        }

        private Payment? ParsePaymentFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<PaymentDto>(json, options);

                if (data == null)
                {
                    _logger.LogWarning("Failed to deserialize payment JSON");
                    return null;
                }

                // Validate required fields
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(data.PaymentNumber))
                    validationErrors.Add("Payment number is missing");

                if (data.Amount <= 0)
                    validationErrors.Add("Payment amount must be greater than zero");

                if (string.IsNullOrWhiteSpace(data.PaymentMethod))
                    validationErrors.Add("Payment method is missing");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Payment validation failed: {Errors}", string.Join(", ", validationErrors));
                    return null;
                }

                // Parse and validate date
                var paymentDate = ParseDate(data.PaymentDate);
                if (!paymentDate.HasValue)
                {
                    _logger.LogWarning("Invalid payment date, using current date");
                    paymentDate = DateTime.Now;
                }

                // Validate payment date is not in the future
                if (paymentDate.Value > DateTime.Now.AddDays(1))
                {
                    _logger.LogWarning("Payment date is in the future, adjusting to today");
                    paymentDate = DateTime.Now;
                }

                var payment = new Payment
                {
                    PaymentNumber = data.PaymentNumber!.Trim(),
                    PaymentDate = paymentDate.Value,
                    Amount = Math.Max(0, data.Amount),
                    PaymentMethod = NormalizePaymentMethod(data.PaymentMethod!),
                    ReferenceNumber = !string.IsNullOrWhiteSpace(data.ReferenceNumber)
                        ? data.ReferenceNumber.Trim()
                        : data.RelatedInvoiceNumber?.Trim(),
                    Notes = BuildPaymentNotes(data.Notes, data.PayerName, data.Confidence, data.Warnings)
                };

                _logger.LogInformation("Successfully parsed payment {PaymentNumber}, Amount: {Amount}, Method: {Method}, Confidence: {Confidence}",
                    payment.PaymentNumber, payment.Amount, payment.PaymentMethod, data.Confidence ?? "unknown");

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing payment JSON: {Json}", json);
                return null;
            }
        }

        private string? ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            email = email.Trim();

            // Basic email validation
            if (email.Contains("@") && email.Contains("."))
                return email;

            return null;
        }

        private string? BuildNotes(string? originalNotes, string? confidence, string? warnings,
            bool multipleInvoicesDetected = false, int totalInvoicesInDocument = 1, int pagesScanned = 1)
        {
            var notesParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(originalNotes))
                notesParts.Add(originalNotes);

            if (!string.IsNullOrWhiteSpace(confidence) && confidence.ToLower() != "high")
                notesParts.Add($"[AI Confidence: {confidence}]");

            if (!string.IsNullOrWhiteSpace(warnings))
                notesParts.Add($"[Warning: {warnings}]");

            // Add multi-page/multi-invoice information
            if (pagesScanned > 1)
                notesParts.Add($"[Pages Scanned: {pagesScanned}]");

            if (multipleInvoicesDetected && totalInvoicesInDocument > 1)
                notesParts.Add($"[⚠️ {totalInvoicesInDocument - 1} additional invoice(s) detected - use Bulk Import mode to extract all]");

            return notesParts.Any() ? string.Join(" | ", notesParts) : null;
        }

        private string? BuildPaymentNotes(string? originalNotes, string? payerName, string? confidence, string? warnings)
        {
            var notesParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(payerName))
                notesParts.Add($"Payer: {payerName}");

            if (!string.IsNullOrWhiteSpace(originalNotes))
                notesParts.Add(originalNotes);

            if (!string.IsNullOrWhiteSpace(confidence) && confidence.ToLower() != "high")
                notesParts.Add($"[AI Confidence: {confidence}]");

            if (!string.IsNullOrWhiteSpace(warnings))
                notesParts.Add($"[Warning: {warnings}]");

            return notesParts.Any() ? string.Join(" | ", notesParts) : null;
        }

        private DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }

        private async Task<byte[]> ReadStreamAsBytesAsync(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Extract multiple invoices from a single PDF file containing many invoices.
        /// Uses a simplified single-call approach for reliability.
        /// </summary>
        public async Task<MultiInvoiceExtractionResult> ExtractMultipleInvoicesFromPdfAsync(
            Stream fileStream,
            string fileName,
            Func<int, int, string, Task>? progressCallback = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new MultiInvoiceExtractionResult();

            try
            {
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    result.Errors.Add("OpenAI API key is not configured");
                    return result;
                }

                await (progressCallback?.Invoke(5, 0, "Reading document...") ?? Task.CompletedTask);

                // Read the file content
                var fileBytes = await ReadStreamAsBytesAsync(fileStream);
                var fileSizeMB = fileBytes.Length / 1024.0 / 1024.0;
                _logger.LogInformation($"Processing multi-invoice PDF: {fileName}, Size: {fileSizeMB:F2} MB");

                if (fileSizeMB > 7)
                {
                    result.Errors.Add($"File too large ({fileSizeMB:F2} MB). Maximum size for bulk processing is 7 MB. Please split the document.");
                    return result;
                }

                var base64File = Convert.ToBase64String(fileBytes);
                var mimeType = GetMimeType(fileName);

                await (progressCallback?.Invoke(15, 0, "Extracting all invoices from document...") ?? Task.CompletedTask);

                // Single API call to extract all invoices at once
                var allInvoices = await ExtractAllInvoicesFromPdfAsync(base64File, mimeType, apiKey);

                if (allInvoices == null || allInvoices.Count == 0)
                {
                    result.Errors.Add("Could not extract any invoices from the document.");
                    return result;
                }

                result.Invoices = allInvoices;
                result.TotalInvoicesDetected = allInvoices.Count;
                result.SuccessfullyExtracted = allInvoices.Count;

                await (progressCallback?.Invoke(90, result.SuccessfullyExtracted, "Finalizing extraction...") ?? Task.CompletedTask);

                // Generate unique invoice numbers if duplicates exist
                var invoiceNumbers = new HashSet<string>();
                int counter = 1;
                foreach (var invoice in result.Invoices)
                {
                    if (string.IsNullOrEmpty(invoice.InvoiceNumber) || invoiceNumbers.Contains(invoice.InvoiceNumber))
                    {
                        invoice.InvoiceNumber = $"BULK-{counter:D4}";
                    }
                    invoiceNumbers.Add(invoice.InvoiceNumber);
                    counter++;
                }

                result.ProcessingTime = DateTime.UtcNow - startTime;
                result.ProcessingSummary = $"Extracted {result.SuccessfullyExtracted} invoices in {result.ProcessingTime.TotalSeconds:F1} seconds.";

                await (progressCallback?.Invoke(100, result.SuccessfullyExtracted, result.ProcessingSummary) ?? Task.CompletedTask);

                _logger.LogInformation(result.ProcessingSummary);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractMultipleInvoicesFromPdfAsync");
                result.Errors.Add($"Processing error: {ex.Message}");
                result.ProcessingTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Extract all invoices from a PDF in a single API call
        /// </summary>
        private async Task<List<Invoice>> ExtractAllInvoicesFromPdfAsync(string base64File, string mimeType, string apiKey)
        {
            var prompt = @"Extract ALL invoices from this PDF document. This document contains multiple invoices.

For EACH invoice found, extract:
- Invoice number
- Invoice date (YYYY-MM-DD format)
- Due date (YYYY-MM-DD format) 
- Supplier/Vendor name (who sent the invoice)
- Supplier address, phone, email if available
- Total amount
- Line items with description, quantity, unit price

Return a JSON array with ALL invoices found. Each invoice object should have this structure:
{
  ""invoiceNumber"": ""string"",
  ""invoiceDate"": ""YYYY-MM-DD"",
  ""dueDate"": ""YYYY-MM-DD"",
  ""supplierName"": ""string"",
  ""supplierAddress"": ""string or null"",
  ""supplierPhone"": ""string or null"",
  ""supplierEmail"": ""string or null"",
  ""totalAmount"": number,
  ""items"": [
    {""description"": ""string"", ""quantity"": number, ""unitPrice"": number}
  ],
  ""notes"": ""string or null""
}

Return ONLY the JSON array, no other text. Example: [{...}, {...}, {...}]";

            try
            {
                _logger.LogInformation("Calling OpenAI API to extract all invoices");
                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType, apiKey);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning("Empty response from bulk invoice extraction");
                    return new List<Invoice>();
                }

                _logger.LogInformation($"Received response of length {response.Length}");
                return ParseMultiInvoiceResponse(response, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting all invoices from PDF");
                throw;
            }
        }

        /// <summary>
        /// Detect the number of invoices in a multi-invoice PDF document
        /// </summary>
        private async Task<(int count, List<string> pageRanges)> DetectInvoiceCountAsync(
            string base64File, string mimeType, string apiKey)
        {
            var prompt = @"Analyze this PDF document and determine:
1. How many separate invoices are contained in this document
2. The page ranges for each invoice (if detectable)

Look for:
- Invoice number changes
- New invoice headers
- Page breaks with new company letterhead
- 'Invoice', 'Bill', 'Tax Invoice' headers that indicate new invoices
- Multiple 'Total' sections indicating separate invoices

Return ONLY a JSON object with this exact structure:
{
  ""totalInvoices"": number,
  ""pageRanges"": [""1-2"", ""3-4"", ...] or [] if not determinable,
  ""confidence"": ""high/medium/low"",
  ""notes"": ""any relevant observations""
}

Be thorough - some documents may contain hundreds of invoices on a single page (e.g., statement summaries).
Return ONLY the JSON object, no other text.";

            try
            {
                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType, apiKey);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning("Empty response from invoice count detection");
                    return (1, new List<string>());
                }

                // Parse the JSON response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonResponse = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    using var doc = JsonDocument.Parse(jsonResponse);
                    var root = doc.RootElement;

                    var count = 1;
                    if (root.TryGetProperty("totalInvoices", out var countProp))
                    {
                        if (countProp.ValueKind == JsonValueKind.Number)
                            count = countProp.GetInt32();
                        else if (countProp.ValueKind == JsonValueKind.String && int.TryParse(countProp.GetString(), out var parsed))
                            count = parsed;
                    }

                    var pageRanges = new List<string>();
                    if (root.TryGetProperty("pageRanges", out var rangesProp) && rangesProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var range in rangesProp.EnumerateArray())
                        {
                            if (range.ValueKind == JsonValueKind.String)
                                pageRanges.Add(range.GetString() ?? "");
                        }
                    }

                    _logger.LogInformation($"Invoice detection: {count} invoices, confidence: {GetJsonString(root, "confidence")}");
                    return (count, pageRanges);
                }

                return (1, new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting invoice count");
                return (1, new List<string>());
            }
        }

        /// <summary>
        /// Extract a batch of invoices from a specific range within the PDF
        /// </summary>
        private async Task<List<Invoice>> ExtractInvoiceBatchFromPdfAsync(
            string base64File, string mimeType, string apiKey,
            int startIndex, int endIndex, int totalInvoices)
        {
            var prompt = $@"This PDF document contains {totalInvoices} invoices. 
Extract ONLY invoices #{startIndex} through #{endIndex} from this document.

For each invoice in this range, extract all details and return a JSON array.
Each invoice should have this structure:
{{
  ""invoiceIndex"": number (1-based position in document),
  ""invoiceNumber"": ""string"",
  ""invoiceDate"": ""YYYY-MM-DD"",
  ""dueDate"": ""YYYY-MM-DD"" or null,
  ""supplierName"": ""string"",
  ""supplierAddress"": ""string"" or null,
  ""supplierPhone"": ""string"" or null,
  ""supplierEmail"": ""string"" or null,
  ""customerName"": ""string"" or null,
  ""customerAddress"": ""string"" or null,
  ""items"": [
    {{
      ""description"": ""string"",
      ""quantity"": number,
      ""unitPrice"": number,
      ""amount"": number
    }}
  ],
  ""subtotal"": number,
  ""taxAmount"": number or 0,
  ""totalAmount"": number,
  ""currency"": ""string"" or ""PGK"",
  ""notes"": ""string"" or null
}}

IMPORTANT:
- Extract EXACTLY invoices {startIndex} to {endIndex}
- Count invoices carefully from the beginning of the document
- Include all line items for each invoice
- Use YYYY-MM-DD format for all dates
- Return ONLY a JSON array of invoice objects, nothing else
- If an invoice is unclear, still include it with available data

Return the JSON array now:";

            try
            {
                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType, apiKey);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning($"Empty response for batch {startIndex}-{endIndex}");
                    return new List<Invoice>();
                }

                return ParseMultiInvoiceResponse(response, startIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting batch {startIndex}-{endIndex}");
                throw;
            }
        }

        /// <summary>
        /// Parse the JSON response containing multiple invoices
        /// </summary>
        private List<Invoice> ParseMultiInvoiceResponse(string response, int batchStartIndex)
        {
            var invoices = new List<Invoice>();

            try
            {
                // Find the JSON array in the response
                var jsonStart = response.IndexOf('[');
                var jsonEnd = response.LastIndexOf(']');

                if (jsonStart < 0 || jsonEnd <= jsonStart)
                {
                    // Try to find a single invoice object
                    jsonStart = response.IndexOf('{');
                    jsonEnd = response.LastIndexOf('}');
                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        // Wrap single object in array
                        response = "[" + response.Substring(jsonStart, jsonEnd - jsonStart + 1) + "]";
                        jsonStart = 0;
                        jsonEnd = response.Length - 1;
                    }
                    else
                    {
                        _logger.LogWarning("Could not find JSON array or object in response");
                        return invoices;
                    }
                }

                var jsonResponse = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonResponse);

                var index = batchStartIndex;
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var invoice = new Invoice
                        {
                            InvoiceNumber = GetJsonString(element, "invoiceNumber") ?? $"INV-{index:D4}",
                            InvoiceDate = ParseJsonDate(element, "invoiceDate") ?? DateTime.Now,
                            DueDate = ParseJsonDate(element, "dueDate") ?? DateTime.Now.AddDays(30),
                            CustomerName = GetJsonString(element, "customerName") ?? GetJsonString(element, "supplierName") ?? "Unknown",
                            CustomerAddress = GetJsonString(element, "customerAddress") ?? GetJsonString(element, "supplierAddress"),
                            CustomerPhone = GetJsonString(element, "customerPhone") ?? GetJsonString(element, "supplierPhone"),
                            CustomerEmail = GetJsonString(element, "customerEmail") ?? GetJsonString(element, "supplierEmail"),
                            TotalAmount = GetJsonDecimal(element, "totalAmount") ?? 0,
                            SubTotal = GetJsonDecimal(element, "subtotal") ?? GetJsonDecimal(element, "totalAmount") ?? 0,
                            GSTAmount = GetJsonDecimal(element, "taxAmount") ?? 0,
                            Notes = GetJsonString(element, "notes") ?? $"Extracted from batch import (Invoice #{index})",
                            Status = "Draft",
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            InvoiceType = "Payable" // Supplier invoices are payable
                        };

                        // Handle supplier info if present
                        var supplierName = GetJsonString(element, "supplierName");
                        if (!string.IsNullOrEmpty(supplierName))
                        {
                            invoice.Supplier = new Supplier
                            {
                                SupplierName = supplierName,
                                Address = GetJsonString(element, "supplierAddress"),
                                Phone = GetJsonString(element, "supplierPhone"),
                                Email = GetJsonString(element, "supplierEmail")
                            };
                        }

                        // Extract line items
                        if (element.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                        {
                            invoice.InvoiceItems = new List<InvoiceItem>();
                            foreach (var item in itemsArray.EnumerateArray())
                            {
                                var invoiceItem = new InvoiceItem
                                {
                                    Description = GetJsonString(item, "description") ?? "Item",
                                    Quantity = (int)(GetJsonDecimal(item, "quantity") ?? 1),
                                    UnitPrice = GetJsonDecimal(item, "unitPrice") ?? GetJsonDecimal(item, "amount") ?? 0
                                };
                                // TotalPrice is computed automatically from Quantity * UnitPrice
                                invoice.InvoiceItems.Add(invoiceItem);
                            }
                        }

                        // Recalculate totals if items exist
                        if (invoice.InvoiceItems != null && invoice.InvoiceItems.Count > 0)
                        {
                            invoice.SubTotal = invoice.InvoiceItems.Sum(i => i.TotalPrice);
                            if (invoice.TotalAmount == 0)
                            {
                                invoice.TotalAmount = invoice.SubTotal + invoice.GSTAmount;
                            }
                        }

                        // PaidAmount defaults to 0, BalanceAmount is computed from TotalAmount - PaidAmount
                        invoices.Add(invoice);
                        index++;
                    }
                    catch (Exception itemEx)
                    {
                        _logger.LogWarning(itemEx, $"Error parsing invoice at index {index}");
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing multi-invoice response");
            }

            return invoices;
        }

        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Safely extract a string from JsonElement
        /// </summary>
        private string? GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        /// <summary>
        /// Safely extract a decimal from JsonElement
        /// </summary>
        private decimal? GetJsonDecimal(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDecimal();
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var val))
                    return val;
            }
            return null;
        }

        /// <summary>
        /// Safely parse a date from JsonElement
        /// </summary>
        private DateTime? ParseJsonDate(JsonElement element, string propertyName)
        {
            var dateStr = GetJsonString(element, propertyName);
            if (string.IsNullOrEmpty(dateStr))
                return null;

            if (DateTime.TryParse(dateStr, out var date))
                return date;

            // Try additional formats
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
                    return date;
            }

            return null;
        }

        // DTOs for JSON deserialization
        private class InvoiceDto
        {
            public string? InvoiceNumber { get; set; }
            public string? InvoiceDate { get; set; }
            public string? DueDate { get; set; }
            public string? CustomerName { get; set; }
            public string? CustomerAddress { get; set; }
            public string? CustomerEmail { get; set; }
            public string? CustomerPhone { get; set; }
            public decimal TotalAmount { get; set; }
            public string? Notes { get; set; }
            public List<InvoiceItemDto>? Items { get; set; }
            public string? Confidence { get; set; }
            public string? Warnings { get; set; }
            // Multi-page detection fields
            public int? PagesScanned { get; set; }
            public bool MultipleInvoicesDetected { get; set; }
            public int? TotalInvoicesInDocument { get; set; }
        }

        private class InvoiceItemDto
        {
            public string? Description { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        private class PaymentDto
        {
            public string? PaymentNumber { get; set; }
            public string? PaymentDate { get; set; }
            public decimal Amount { get; set; }
            public string? PaymentMethod { get; set; }
            public string? ReferenceNumber { get; set; }
            public string? RelatedInvoiceNumber { get; set; }
            public string? PayerName { get; set; }
            public string? Notes { get; set; }
            public string? Confidence { get; set; }
            public string? Warnings { get; set; }
        }
    }
}

