public static class LdConstant
{
    public const string TAG_COMMENT = "0";
    public const string TAG_COLOR = "!COLOUR";
    public const string TAG_CODE = "CODE";
    public const string TAG_VALUE = "VALUE";
    public const string TAG_EDGE = "EDGE";
    public const string TAG_ALPHA = "ALPHA";

    public const string TAG_FILE = "FILE";
    public const string TAG_BFC = "BFC";
    public const string TAG_CERTIFY = "CERTIFY";
    public const string TAG_NOCERTIFY = "NOCERTIFY";
    public const string TAG_CCW = "CCW";
    public const string TAG_CW = "CW";
    public const string TAG_INVERTNEXT = "INVERTNEXT";

    public const short LD_COLOR_MAIN = 16;
    public const short LD_COLOR_EDGE = 24;

    public const float LDU_IN_MM = 0.4f;

    public const string TAG_MPD_FILE_EXT = ".mpd";
    public const string TAG_LDR_FILE_EXT = ".ldr";
    public const string TAG_DAT_FILE_EXT = ".dat";

    // Import path
    public const string LD_CONFIG_FNAME = "ldconfig.ldr";
    public const string LD_PARTS_PATH = "ldparts";
    public const string PARTS_LIST_FNAME = "parts.lst";
    public const string PARTS_PATH_LIST_FNAME = "partspath.lst";

    static public short GetEffectiveColorIndex(short localColor, short parentColor)
    {
        return (localColor != LdConstant.LD_COLOR_MAIN) ? localColor : parentColor;
    }
}
