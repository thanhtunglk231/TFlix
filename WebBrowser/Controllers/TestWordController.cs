using Aspose.Words;
using Aspose.Words.Saving;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using Document = Aspose.Words.Document;

namespace WebBrowser.Controllers
{
    public class TestWordController : Controller
    {
        private readonly Services.EmailService _emailService;

        public TestWordController(Services.EmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertWord(IFormFile wordFile, string? email = "thanhtung23032003@gmail.com")
        {
            if (wordFile == null || wordFile.Length == 0)
            {
                ViewBag.Error = "Chưa chọn file Word.";
                return View("Index");
            }

            if (!wordFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Chỉ hỗ trợ file .docx";
                return View("Index");
            }

            string html = ConvertDocxToHtmlString(wordFile);
            html = RemoveAsposeEvaluationText(html);
            html = FixParagraphHasTwoImages(html);

            ViewBag.Html = html;

            // Nếu có email thì gửi
            if (!string.IsNullOrWhiteSpace(email))
            {
                await _emailService.SendAsync(email, "Kết quả render Word", html);
                ViewBag.EmailSent = $"Đã gửi email tới {email}";
            }

            return View("Index");
        }

        private string ConvertDocxToHtmlString(IFormFile formFile)
        {
            using var ms = new MemoryStream();
            formFile.CopyTo(ms);
            ms.Position = 0;

            var doc = new Document(ms);

            var options = new HtmlSaveOptions(SaveFormat.Html)
            {
                ExportImagesAsBase64 = true,
                CssStyleSheetType = CssStyleSheetType.Inline,
                PrettyFormat = true
            };

            using var htmlStream = new MemoryStream();
            doc.Save(htmlStream, options);
            htmlStream.Position = 0;

            using var reader = new StreamReader(htmlStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private string RemoveAsposeEvaluationText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var hfDivs = doc.DocumentNode.SelectNodes("//div[contains(@style,'-aw-headerfooter-type')]");
            if (hfDivs != null)
            {
                foreach (var div in hfDivs.ToList())
                    div.Remove();
            }

            return doc.DocumentNode.OuterHtml;
        }

        private string FixParagraphHasTwoImages(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var ps = doc.DocumentNode.SelectNodes("//p[.//img]");
            if (ps == null)
                return html;

            foreach (var p in ps)
            {
                var imgs = p.SelectNodes(".//img")?.ToList();

                // Bắt đúng thẻ p có 2 ảnh: dấu + chữ ký
                if (imgs == null || imgs.Count != 2)
                    continue;

                // Chỉ sửa CSS thẻ p
                p.SetAttributeValue(
                    "style",
                    "margin-top: 0.65pt; margin-left: 53.75pt; margin-bottom: 0pt; font-size: 9pt;"
                );

                // Thêm CSS cho ảnh thứ 2
                var img2 = imgs[1];

                var oldStyle = img2.GetAttributeValue("style", "");

                if (!string.IsNullOrWhiteSpace(oldStyle) && !oldStyle.Trim().EndsWith(";"))
                    oldStyle += ";";

                oldStyle += "margin-left:41px;";

                img2.SetAttributeValue("style", oldStyle);

                break;
            }

            return doc.DocumentNode.OuterHtml;
        }
    }
}