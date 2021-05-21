using System;

namespace HTMLParserDemo
{
    public class WebsiteToParse
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsUrl { get; set; }
        public string[] FilenameWordsToSkip { get; set; }

        public string BaseContainerTag { get; set; }
        public string InnerTitleTag { get; set; }
        public string InnerImageTag { get; set; }
        public string InnerTextTag { get; set; }
    }
}
