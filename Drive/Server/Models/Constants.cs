using System;


namespace Server.Models
{
    public class Constants
    {
        public const int MessageHeaderLen = 5;
        public const int DataLengthFieldLen = 4;
        public const int CodeLen = 1;
    }

    public enum MessageCode
    {
        Login = 0,
        SignUp,

    }
    public enum ResponseCondition
    {
        Failure = 0,
        Success,
    }

        

}
