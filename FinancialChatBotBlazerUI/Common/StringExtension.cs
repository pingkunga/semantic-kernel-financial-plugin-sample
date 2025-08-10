using Markdig;

namespace FinancialChatBotBlazerUI.Common
{
    public static class StringExtensions
    {
        private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder()
            .ConfigureNewLine("\n")
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

        public static string ToHtml(this string markdown) =>
            string.IsNullOrWhiteSpace(markdown) is false
                ? Markdown.ToHtml(markdown, s_pipeline)
                : "";
    }
}
