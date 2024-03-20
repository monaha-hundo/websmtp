namespace websmtp.services;

public enum SpfVerifyResult
{
    None,
    Neutral,
    Pass,
    Fail,
    Softfail,
    Temperror,
    Permerror
}

