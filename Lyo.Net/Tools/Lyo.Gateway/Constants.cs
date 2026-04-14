namespace Lyo.Gateway;

public static class Constants
{
    public static class Page
    {
        public const string Cache = "cache";

        public const string Locks = "locks";

        public const string QueryBuilder = "query-builder";

        public const string RichTextEditor = "rich-text-editor";

        public const string FileService = "file-service";

        public const string FileStorageWorkbench = "filestorage-workbench";

        public const string HtmlToPdf = "html-to-pdf";

        public const string RabbitMq = "rabbitmq";

        public const string Metrics = "metrics";

        public const string PdfAnnotator = "pdf-annotator";

        public const string QrCodeGenerator = "qr-code-generator";

        public const string BarcodeGenerator = "barcode-generator";

        public const string SpriteSheetAnimator = "spritesheet-animator";

        public const string ImageWorkbench = "image-workbench";

        public const string TextDiff = "text-diff";

        public const string Translation = "translation";

        public const string Profanity = "profanity";

        public const string Tts = "tts";

        public const string Messaging = "messaging";

        /// <summary>Single workbench for CSV + XLSX (tabs). Legacy routes <c>/csv</c> and <c>/xlsx</c> also resolve here.</summary>
        public const string CsvXlsx = "csv-xlsx";
    }
}