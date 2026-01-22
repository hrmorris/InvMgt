using System.Text;
using System.Text.Json;
using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    /// <summary>
    /// AI-Powered Document Processing Service
    /// Uses Google Gemini 2.5 Pro for advanced document analysis and data extraction
    /// Features: Advanced OCR, intelligent data extraction, contextual understanding, multi-format support
    /// API Version: v1beta
    /// </summary>
    public class AiProcessingService : IAiProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiProcessingService> _logger;
        private string? _apiKey;

        public AiProcessingService(HttpClient httpClient, IConfiguration configuration, ILogger<AiProcessingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["GoogleAI:ApiKey"];
        }

        public async Task<Invoice?> ExtractInvoiceFromFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("Google AI API key is not configured. Please add GoogleAI:ApiKey to appsettings.json");
                    throw new InvalidOperationException("Google AI API key is not configured");
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

ADVANCED SCANNING PROTOCOL:
1. DOCUMENT STRUCTURE ANALYSIS:
   - Identify document layout, headers, footers, and sections
   - Detect tables, columns, and data relationships
   - Recognize logos, watermarks, stamps, and signatures
   - Read both printed text and handwritten annotations
   - Process multi-column layouts and complex formatting

2. INTELLIGENT DATA EXTRACTION:
   - Extract SUPPLIER/VENDOR information (the company sending the invoice, usually at the top)
   - Distinguish between 'Bill From' (supplier) and 'Bill To' (buyer/recipient)
   - Use contextual understanding to identify invoice elements
   - Cross-reference multiple data points for accuracy
   - Detect and handle multiple date formats automatically
   - Identify currency symbols and decimal separators
   - Extract data from tables with merged cells or irregular layouts

3. QUALITY ASSURANCE:
   - Verify mathematical accuracy of all calculations
   - Cross-check totals against line item sums
   - Validate date logic (invoice date ≤ due date)
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
- Line Items: Extract ALL items including hidden details, notes, SKU/part numbers
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
  ""totalAmount"": decimal number (required),
  ""notes"": ""string or null"",
  ""items"": [
    {
      ""description"": ""string (detailed description)"",
      ""quantity"": integer (must be >= 1),
      ""unitPrice"": decimal (price per unit)
    }
  ],
  ""confidence"": ""high/medium/low (your confidence in extraction)"",
  ""warnings"": ""string or null (any issues noticed)""
}

VALIDATION RULES:
- All required fields must have values
- Dates must be valid and logical (invoice date <= due date)
- All amounts must be positive numbers
- Quantity must be >= 1
- Total should match sum of (quantity × unitPrice) for all items
- If total doesn't match, note it in warnings
- customerName should contain the SUPPLIER name (who is billing), not the buyer

Return ONLY the JSON object, nothing else.";

                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType);

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
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("Google AI API key is not configured");
                    return null;
                }

                var fileBytes = await ReadStreamAsBytesAsync(fileStream);
                var base64File = Convert.ToBase64String(fileBytes);
                var mimeType = GetMimeType(fileName);

                // ENHANCED Gemini 2.0 Flash Prompt - Specialized for PNG Bank Statements (BSP, Westpac, ANZ, Kina Bank)
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

                var response = await CallGeminiVisionApiAsync(prompt, base64File, mimeType);

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
                if (string.IsNullOrEmpty(_apiKey))
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

                var response = await CallGeminiTextApiAsync(prompt);
                return response ?? "No match found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching payment to invoice");
                return "Error in matching";
            }
        }

        private async Task<string?> CallGeminiVisionApiAsync(string prompt, string base64Image, string mimeType)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 8192  // Pro has higher token limit
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new InvalidOperationException($"Google AI API authentication failed. Please check your API key. Status: {response.StatusCode}");
                    }

                    throw new Exception($"Google AI API error ({response.StatusCode}): {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Gemini API response length: {responseContent.Length} characters");

                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Check if response has error
                if (result.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                    _logger.LogError($"API returned error: {errorMessage}");
                    throw new Exception($"Google AI API error: {errorMessage}");
                }

                if (result.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];

                    // Check finish reason
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        _logger.LogInformation($"API finish reason: {reason}");

                        if (reason != "STOP")
                        {
                            _logger.LogWarning($"API stopped with reason: {reason}. Content may be blocked or incomplete.");
                            if (reason == "SAFETY")
                            {
                                throw new Exception("Content was blocked by safety filters. Please try a different file.");
                            }
                        }
                    }

                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
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
                _logger.LogError(ex, "Error calling Gemini Vision API");
                throw new Exception($"Failed to call Google AI API: {ex.Message}", ex);
            }
        }

        private async Task<string?> CallGeminiTextApiAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 2048  // Pro has higher token limit
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        return CleanJsonResponse(parts[0].GetProperty("text").GetString());
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini Text API");
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
                    Notes = BuildNotes(data.Notes, data.Confidence, data.Warnings),
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

        private string? BuildNotes(string? originalNotes, string? confidence, string? warnings)
        {
            var notesParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(originalNotes))
                notesParts.Add(originalNotes);

            if (!string.IsNullOrWhiteSpace(confidence) && confidence.ToLower() != "high")
                notesParts.Add($"[AI Confidence: {confidence}]");

            if (!string.IsNullOrWhiteSpace(warnings))
                notesParts.Add($"[Warning: {warnings}]");

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

