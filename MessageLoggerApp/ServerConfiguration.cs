namespace MessageLoggerApp;

public class ServerConfiguration
{
    public string Address { get; set; }
    public int LoginPort { get; set; }
    public int RcvMsgPort { get; set; }
    public string Salt { get; set; }
}
