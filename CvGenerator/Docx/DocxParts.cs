namespace CvGenerator.Docx;

/// <summary>The fixed parts of the WordprocessingML package.</summary>
internal static class DocxParts
{
    public const string ContentTypes = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Default Extension="png" ContentType="image/png"/>
          <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
          <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
          <Override PartName="/word/settings.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"/>
          <Override PartName="/word/fontTable.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml"/>
          <Override PartName="/word/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
          <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
          <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
        </Types>
        """;

    public const string RootRels = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
        </Relationships>
        """;

    public const string DocumentRels = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId0" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/fontTable" Target="fontTable.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings" Target="settings.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="theme/theme1.xml"/>
          <Relationship Id="rIdIcon-phone" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-phone.png"/>
          <Relationship Id="rIdIcon-email" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-email.png"/>
          <Relationship Id="rIdIcon-pin" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-pin.png"/>
          <Relationship Id="rIdIcon-link" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-link.png"/>
          <Relationship Id="rIdIcon-calendar" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-calendar.png"/>
        </Relationships>
        """;

    public const string Settings = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:settings xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:defaultTabStop w:val="708"/>
          <w:characterSpacingControl w:val="doNotCompress"/>
          <w:clrSchemeMapping w:bg1="light1" w:t1="dark1" w:bg2="light2" w:t2="dark2" w:accent1="accent1" w:accent2="accent2" w:accent3="accent3" w:accent4="accent4" w:accent5="accent5" w:accent6="accent6" w:hyperlink="hyperlink" w:followedHyperlink="followedHyperlink"/>
        </w:settings>
        """;

    /// <summary>
    /// Declares Carlito — metric-compatible with Calibri — as the substitute for
    /// machines without Calibri installed (typically LibreOffice on Linux).
    /// </summary>
    public const string FontTable = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:fonts xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:font w:name="Calibri"><w:altName w:val="Carlito"/><w:family w:val="swiss"/><w:pitch w:val="variable"/></w:font>
        </w:fonts>
        """;

    /// <summary>
    /// A monochrome Office theme. The styles reference these slots, so Word's
    /// Design &gt; Colors and Design &gt; Fonts menus restyle the whole document.
    /// </summary>
    public const string Theme = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="CV Monochrome">
          <a:themeElements>
            <a:clrScheme name="CV Monochrome">
              <a:dk1><a:srgbClr val="1A1A1A"/></a:dk1>
              <a:lt1><a:srgbClr val="FFFFFF"/></a:lt1>
              <a:dk2><a:srgbClr val="3D3D3D"/></a:dk2>
              <a:lt2><a:srgbClr val="F2F2F2"/></a:lt2>
              <a:accent1><a:srgbClr val="7A7A7A"/></a:accent1>
              <a:accent2><a:srgbClr val="BFBFBF"/></a:accent2>
              <a:accent3><a:srgbClr val="595959"/></a:accent3>
              <a:accent4><a:srgbClr val="404040"/></a:accent4>
              <a:accent5><a:srgbClr val="262626"/></a:accent5>
              <a:accent6><a:srgbClr val="A6A6A6"/></a:accent6>
              <a:hlink><a:srgbClr val="2B579A"/></a:hlink>
              <a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
            </a:clrScheme>
            <a:fontScheme name="CV Monochrome">
              <a:majorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont>
              <a:minorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont>
            </a:fontScheme>
            <a:fmtScheme name="Office">
              <a:fillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:fillStyleLst>
              <a:lnStyleLst>
                <a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
                <a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
                <a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
              </a:lnStyleLst>
              <a:effectStyleLst>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
              </a:effectStyleLst>
              <a:bgFillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:bgFillStyleLst>
            </a:fmtScheme>
          </a:themeElements>
        </a:theme>
        """;

    public const string AppProperties = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties">
          <Application>cv-generator</Application>
        </Properties>
        """;

    public static string Styles { get; } = $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:docDefaults>
            <w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:asciiTheme="minorHAnsi" w:hAnsiTheme="minorHAnsi"/><w:color w:val="{DocxRenderer.Ink}" w:themeColor="text2"/><w:sz w:val="20"/><w:szCs w:val="20"/></w:rPr></w:rPrDefault>
            <w:pPrDefault><w:pPr><w:spacing w:after="80"/></w:pPr></w:pPrDefault>
          </w:docDefaults>
          <w:style w:type="paragraph" w:styleId="Normal" w:default="1"><w:name w:val="Normal"/><w:uiPriority w:val="0"/><w:qFormat/></w:style>
          <w:style w:type="paragraph" w:styleId="Name"><w:name w:val="CV Name"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="1"/><w:qFormat/><w:pPr><w:spacing w:after="40"/></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Black}" w:themeColor="text1"/><w:sz w:val="54"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Headline"><w:name w:val="Headline"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="2"/><w:qFormat/><w:pPr><w:spacing w:after="60"/></w:pPr><w:rPr><w:color w:val="{DocxRenderer.Muted}" w:themeColor="accent1"/><w:sz w:val="26"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="ContactLine"><w:name w:val="Contact Line"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="3"/><w:qFormat/><w:pPr><w:spacing w:after="200"/></w:pPr><w:rPr><w:b/><w:sz w:val="20"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Heading"><w:name w:val="Section Heading"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="4"/><w:qFormat/><w:pPr><w:spacing w:before="200" w:after="120"/><w:pBdr><w:bottom w:val="single" w:sz="18" w:space="4" w:color="{DocxRenderer.Black}" w:themeColor="text1"/></w:pBdr></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Black}" w:themeColor="text1"/><w:sz w:val="28"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="EntryTitle"><w:name w:val="Entry Title"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="5"/><w:qFormat/><w:pPr><w:spacing w:before="40" w:after="20"/></w:pPr><w:rPr><w:color w:val="{DocxRenderer.Title}"/><w:sz w:val="26"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Company"><w:name w:val="Company"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="6"/><w:qFormat/><w:pPr><w:spacing w:after="20"/></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Muted}" w:themeColor="accent1"/><w:sz w:val="22"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Meta"><w:name w:val="Meta"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="7"/><w:pPr><w:spacing w:after="80"/></w:pPr><w:rPr><w:color w:val="{DocxRenderer.Muted}" w:themeColor="accent1"/><w:sz w:val="20"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Body"><w:name w:val="Body"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="8"/><w:qFormat/><w:pPr><w:spacing w:after="100"/></w:pPr></w:style>
          <w:style w:type="paragraph" w:styleId="Bullet"><w:name w:val="Bullet"/><w:basedOn w:val="Body"/><w:uiPriority w:val="9"/><w:pPr><w:spacing w:after="40"/></w:pPr></w:style>
          <w:style w:type="paragraph" w:styleId="SkillGroup"><w:name w:val="Skill Group"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="10"/><w:pPr><w:spacing w:before="120" w:after="60"/></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Muted}" w:themeColor="accent1"/><w:sz w:val="18"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Skill"><w:name w:val="Skill"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="11"/><w:qFormat/><w:pPr><w:spacing w:after="100"/><w:pBdr><w:bottom w:val="single" w:sz="4" w:space="6" w:color="{DocxRenderer.Rule}" w:themeColor="accent2"/></w:pBdr></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Title}"/><w:sz w:val="20"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="DotBreak"><w:name w:val="Dotted Break"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="12"/><w:semiHidden/><w:pPr><w:spacing w:before="40" w:after="120"/><w:pBdr><w:bottom w:val="dotted" w:sz="4" w:space="2" w:color="{DocxRenderer.Rule}" w:themeColor="accent2"/></w:pBdr></w:pPr><w:rPr><w:sz w:val="8"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="Subject"><w:name w:val="Subject"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="13"/><w:qFormat/><w:pPr><w:spacing w:before="90" w:after="80"/></w:pPr><w:rPr><w:b/><w:color w:val="{DocxRenderer.Black}" w:themeColor="text1"/><w:sz w:val="22"/></w:rPr></w:style>
          <w:style w:type="paragraph" w:styleId="SignOff"><w:name w:val="Sign Off"/><w:basedOn w:val="Body"/><w:uiPriority w:val="14"/><w:pPr><w:spacing w:after="900"/></w:pPr></w:style>
          <w:style w:type="paragraph" w:styleId="NoGap"><w:name w:val="No Gap"/><w:basedOn w:val="Body"/><w:uiPriority w:val="15"/><w:pPr><w:spacing w:after="0"/></w:pPr></w:style>
        </w:styles>
        """;
}
