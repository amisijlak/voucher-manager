using System.Globalization;
using System.Text;
using VoucherMgt.Common;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web;

namespace VoucherMgt.Web.Documents;

public static class RequisitionFiles
{
    public static byte[] Csv(Requisition item)
    {
        var builder = new StringBuilder();
        builder.Append('\uFEFF');
        Row(builder, "Tracking number", item.Number);
        Row(builder, "Title", item.Title);
        Row(builder, "Status", StatusText.Label(item.Status));
        Row(builder, "Period", $"{item.PeriodStart:dd MMM yyyy} - {item.PeriodEnd:dd MMM yyyy}");
        Row(builder, "Department", item.Department ?? "");
        Row(builder, "Requested by", item.RequestedByName);
        Row(builder, "Purpose", item.Purpose ?? "");
        Row(builder, "Budget", item.BudgetPeriod?.Name ?? "");
        builder.AppendLine();
        Row(builder, "#", "Item", "Qty", "Unit amount", "Currency", "Amount");
        foreach (var line in item.Lines.OrderBy(l => l.LineNumber))
        {
            Row(builder,
                line.LineNumber.ToString(CultureInfo.InvariantCulture),
                line.Description,
                line.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
                Money.FormatNumber(line.UnitAmount, line.Currency),
                line.Currency,
                Money.FormatNumber(line.Amount, line.Currency));
        }

        builder.AppendLine();
        Row(builder, "", "", "", "", "Total UGX", Money.FormatNumber(item.RequestedUgx, Money.Ugx));
        if (item.RequestedUsd != 0)
        {
            Row(builder, "", "", "", "", "Total USD", Money.FormatNumber(item.RequestedUsd, Money.Usd));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static byte[] Pdf(Requisition item, string organizationName)
    {
        var document = new PdfDocument();
        document.AddHeader(organizationName, item);
        document.AddTable(item);
        document.AddTotals(item);
        return document.Build();
    }

    private static void Row(StringBuilder builder, params string[] cells) =>
        builder.AppendLine(string.Join(",", cells.Select(Escape)));

    private static string Escape(string? value)
    {
        var text = value ?? "";
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }

    private sealed class PdfDocument
    {
        private const float PageWidth = 595f;
        private const float PageHeight = 842f;
        private const float Left = 40f;
        private const float Right = 555f;
        private readonly List<List<byte[]>> _pages = [[Array.Empty<byte>()]];
        private float _y = PageHeight - 48f;

        public void AddHeader(string organizationName, Requisition item)
        {
            FillRgb(0, PageHeight - 108, PageWidth, 108, 0.063f, 0.157f, 0.200f);
            FillRgb(0, PageHeight - 116, PageWidth, 8, 0.769f, 0.639f, 0.353f);
            Text(Left, PageHeight - 38, organizationName, 11, bold: true, r: 0.953f, g: 0.886f, b: 0.722f);
            Text(Left, PageHeight - 64, "PETTY CASH REQUISITION", 18, bold: true, r: 1, g: 1, b: 1);
            var numberWidth = TextWidth(item.Number, 11) + 18;
            FillRgb(Right - numberWidth, PageHeight - 78, numberWidth + 6, 22, 0.961f, 0.773f, 0.094f);
            Text(Right + 2, PageHeight - 72, item.Number, 11, bold: true, alignRight: true, r: 0.169f, g: 0.133f, b: 0);
            _y = PageHeight - 146;
            Text(Left, _y, item.Title, 13, bold: true, r: 0.063f, g: 0.157f, b: 0.200f);
            _y -= 20;
            Meta("Status", StatusText.Label(item.Status));
            Meta("Period", $"{item.PeriodStart:dd MMM yyyy} - {item.PeriodEnd:dd MMM yyyy}");
            if (!string.IsNullOrWhiteSpace(item.Department))
            {
                Meta("Department", item.Department);
            }

            Meta("Requested by", item.RequestedByName);
            if (!string.IsNullOrWhiteSpace(item.Purpose))
            {
                Meta("Purpose", item.Purpose);
            }

            if (item.BudgetPeriod is not null)
            {
                Meta("Budget", item.BudgetPeriod.Name);
            }

            _y -= 10;
        }

        public void AddTable(Requisition item)
        {
            DrawHeader();
            var shade = false;
            foreach (var line in item.Lines.OrderBy(l => l.LineNumber))
            {
                var wrapped = Wrap(line.Description, 230f, 9).ToList();
                if (wrapped.Count == 0)
                {
                    wrapped.Add("");
                }

                var rowHeight = 8f + (wrapped.Count * 12f);
                Ensure(rowHeight);
                if (shade)
                {
                    FillRgb(Left, _y - rowHeight + 10f, Right - Left, rowHeight, 0.973f, 0.957f, 0.925f);
                }

                shade = !shade;
                var textY = _y;
                Text(68f, textY, line.LineNumber.ToString(CultureInfo.InvariantCulture), 9, bold: false, alignRight: true);
                Text(48f, textY, wrapped[0], 9, bold: false);
                Text(390f, textY, line.Quantity.ToString("#,##0.##", CultureInfo.InvariantCulture), 9, bold: false, alignRight: true);
                Text(470f, textY, Money.FormatNumber(line.UnitAmount, line.Currency), 9, bold: false, alignRight: true);
                Text(Right, textY, Money.Format(line.Amount, line.Currency), 9, bold: false, alignRight: true);
                for (var i = 1; i < wrapped.Count; i++)
                {
                    textY -= 12f;
                    Text(48f, textY, wrapped[i], 9, bold: false);
                }

                _y -= rowHeight;
            }

            Line(Left, _y + 6f, Right, _y + 6f);
        }

        public void AddTotals(Requisition item)
        {
            var rows = 1 + (item.RequestedUsd != 0 ? 1 : 0);
            if (item.ApprovedUgx != 0 || item.ApprovedUsd != 0)
            {
                rows += 1 + (item.ApprovedUsd != 0 ? 1 : 0);
            }

            Ensure(18f + (rows * 16f));
            _y -= 6;
            var boxHeight = 12f + (rows * 16f);
            FillRgb(330, _y - boxHeight + 14f, Right - 330, boxHeight, 0.063f, 0.157f, 0.200f);
            Total("Requested UGX", Money.FormatNumber(item.RequestedUgx, Money.Ugx));
            if (item.RequestedUsd != 0)
            {
                Total("Requested USD", Money.FormatNumber(item.RequestedUsd, Money.Usd));
            }

            if (item.ApprovedUgx != 0 || item.ApprovedUsd != 0)
            {
                Total("Approved UGX", Money.FormatNumber(item.ApprovedUgx, Money.Ugx));
                if (item.ApprovedUsd != 0)
                {
                    Total("Approved USD", Money.FormatNumber(item.ApprovedUsd, Money.Usd));
                }
            }
        }

        public byte[] Build()
        {
            var objects = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
                Array.Empty<byte>()
            };
            var fontRegular = objects.Count + 1;
            objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
            var fontBold = objects.Count + 1;
            objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));

            var pageIds = new List<int>();
            foreach (var content in _pages)
            {
                var stream = content.SelectMany(chunk => chunk).ToArray();
                var contentId = objects.Count + 1;
                objects.Add(Stream(stream));
                var pageId = objects.Count + 1;
                pageIds.Add(pageId);
                objects.Add(Encoding.ASCII.GetBytes(
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} {PageHeight.ToString(CultureInfo.InvariantCulture)}] /Contents {contentId} 0 R /Resources << /Font << /F1 {fontRegular} 0 R /F2 {fontBold} 0 R >> >> >>"));
            }

            var kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
            objects[1] = Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{kids}] /Count {pageIds.Count} >>");

            using var output = new MemoryStream();
            var header = "%PDF-1.4\n"u8.ToArray();
            output.Write(header);
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(output.Position);
                var start = Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n");
                output.Write(start);
                output.Write(objects[i]);
                output.Write("\nendobj\n"u8);
            }

            var xref = output.Position;
            output.Write(Encoding.ASCII.GetBytes($"xref\n0 {objects.Count + 1}\n"));
            output.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));
            foreach (var offset in offsets.Skip(1))
            {
                output.Write(Encoding.ASCII.GetBytes($"{offset:0000000000} 00000 n \n"));
            }

            output.Write(Encoding.ASCII.GetBytes($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"));
            return output.ToArray();
        }

        private void DrawHeader()
        {
            Ensure(22f);
            FillRgb(Left, _y - 6f, Right - Left, 18f, 0.106f, 0.227f, 0.294f);
            Text(68f, _y, "#", 9, bold: true, alignRight: true, r: 0.953f, g: 0.886f, b: 0.722f);
            Text(48f, _y, "Item", 9, bold: true, r: 1, g: 1, b: 1);
            Text(390f, _y, "Qty", 9, bold: true, alignRight: true, r: 1, g: 1, b: 1);
            Text(470f, _y, "Unit", 9, bold: true, alignRight: true, r: 1, g: 1, b: 1);
            Text(Right, _y, "Amount", 9, bold: true, alignRight: true, r: 0.953f, g: 0.886f, b: 0.722f);
            _y -= 20f;
        }

        private void Meta(string label, string value)
        {
            foreach (var part in Wrap(value, 430f, 10))
            {
                Ensure(14f);
                Text(Left, _y, label, 10, bold: true, r: 0.541f, g: 0.408f, b: 0.075f);
                Text(130f, _y, part, 10, bold: false, r: 0.125f, g: 0.141f, b: 0.145f);
                label = "";
                _y -= 14f;
            }
        }

        private void Total(string label, string amount)
        {
            Ensure(16f);
            Text(348f, _y, label, 10, bold: true, r: 0.953f, g: 0.886f, b: 0.722f);
            Text(Right - 8f, _y, amount, 10, bold: true, alignRight: true, r: 1, g: 1, b: 1);
            _y -= 16f;
        }

        private void Ensure(float height)
        {
            if (_y - height > 48f)
            {
                return;
            }

            _pages.Add([]);
            _y = PageHeight - 48f;
            DrawHeader();
        }

        private void FillRgb(float x, float y, float width, float height, float red, float green, float blue)
        {
            Op($"{Num(red)} {Num(green)} {Num(blue)} rg\n{Num(x)} {Num(y)} {Num(width)} {Num(height)} re\nf\n0 g\n");
        }

        private void Line(float x1, float y1, float x2, float y2) =>
            Op($"{Num(x1)} {Num(y1)} m {Num(x2)} {Num(y2)} l S\n");

        private void Text(float x, float y, string text, float size, bool bold, bool alignRight = false, float r = 0, float g = 0, float b = 0)
        {
            if (alignRight)
            {
                x -= TextWidth(text, size);
            }

            var font = bold ? "/F2" : "/F1";
            var command = $"BT\n{Num(r)} {Num(g)} {Num(b)} rg\n{font} {Num(size)} Tf\n{Num(x)} {Num(y)} Td\n";
            Op(Encoding.ASCII.GetBytes(command));
            Op(PdfString(text));
            Op(" Tj\nET\n0 g\n"u8.ToArray());
        }

        private void Op(string text) => Op(Encoding.ASCII.GetBytes(text));

        private void Op(byte[] bytes) => _pages[^1].Add(bytes);

        private static IEnumerable<string> Wrap(string text, float maxWidth, float size)
        {
            var words = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                yield break;
            }

            var line = words[0];
            for (var i = 1; i < words.Length; i++)
            {
                var next = line + " " + words[i];
                if (TextWidth(next, size) <= maxWidth)
                {
                    line = next;
                }
                else
                {
                    yield return line;
                    line = words[i];
                }
            }

            yield return line;
        }

        private static float TextWidth(string text, float size)
        {
            var width = 0;
            foreach (var character in text)
            {
                width += character switch
                {
                    ' ' => 278,
                    ',' or '.' or ':' or ';' => 278,
                    '-' or '–' or '—' => 333,
                    >= '0' and <= '9' => 556,
                    >= 'A' and <= 'Z' => 720,
                    _ => 500
                };
            }

            return width * size / 1000f;
        }

        private static string Num(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static byte[] PdfString(string text)
        {
            var raw = Encoding.GetEncoding(1252).GetBytes(text ?? "");
            using var stream = new MemoryStream();
            stream.WriteByte((byte)'(');
            foreach (var value in raw)
            {
                if (value is (byte)'\\' or (byte)'(' or (byte)')')
                {
                    stream.WriteByte((byte)'\\');
                    stream.WriteByte(value);
                }
                else if (value is < 32 or > 126)
                {
                    stream.WriteByte((byte)'\\');
                    foreach (var digit in Convert.ToString(value, 8).PadLeft(3, '0'))
                    {
                        stream.WriteByte((byte)digit);
                    }
                }
                else
                {
                    stream.WriteByte(value);
                }
            }

            stream.WriteByte((byte)')');
            return stream.ToArray();
        }

        private static byte[] Stream(byte[] content)
        {
            var prefix = Encoding.ASCII.GetBytes($"<< /Length {content.Length} >>\nstream\n");
            var suffix = "\nendstream"u8.ToArray();
            var combined = new byte[prefix.Length + content.Length + suffix.Length];
            prefix.CopyTo(combined, 0);
            content.CopyTo(combined, prefix.Length);
            suffix.CopyTo(combined, prefix.Length + content.Length);
            return combined;
        }
    }
}
