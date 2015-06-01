namespace StackExchange.Opserver
{
    /// <summary>
    /// From Marc Gravell - every time a split is used you can either allocate an array
    /// ...or reference the single one created here and avoid that 
    /// </summary>
    public static class StringSplits
    {
        public static readonly char[] Space = {' '},
                                      Comma = {','},
                                      Period = {'.'},
                                      Minus = {'-'},
                                      Plus = {'+'},
                                      Asterisk = {'*'},
                                      Percent = {'%'},
                                      Ampersand = {'&'},
                                      AtSign = {'@'},
                                      Equal = {'='},
                                      Underscore = {'_'},
                                      NewLine = {'\n'},
                                      SemiColon = {';'},
                                      Colon = {':'},
                                      VerticalBar = {'|'},
                                      ForwardSlash = {'/'},
                                      BackSlash = {'\\'},
                                      DoubleQuote = { '"' },
                                      Period_Plus = { '.', '+' },
                                      NewLine_CarriageReturn = {'\n', '\r'},
                                      Comma_SemiColon = {',', ';'},
                                      Comma_SemiColon_Space = {',', ';', ' '},
                                      BackSlash_Slash_Period = {'\\', '/', '.'};
    }
}
