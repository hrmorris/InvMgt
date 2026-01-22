-- Update existing invoices to properly calculate SubTotal, GST, and new TotalAmount
-- This assumes the current TotalAmount in the database is the actual subtotal (before GST)
UPDATE Invoices
SET SubTotal = TotalAmount,
    GSTAmount = ROUND(TotalAmount * 0.10, 2),
    TotalAmount = ROUND(TotalAmount + (TotalAmount * 0.10), 2),
    GSTRate = 10.0
WHERE SubTotal = 0
    OR SubTotal IS NULL;