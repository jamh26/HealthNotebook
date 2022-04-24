namespace HealthNotebook.Configuration.Messages;

public static class ErrorMessages
{
    public static class Generic
    {
        public static string ObjectNotFound = "Object not found";
        public static string InvalidRequest = "InvalidRequest";
        public static string SomethingWentWrong = "Something went wrong, please try again later";
        public static string UnableToProcess = "Unable to process request";
        public static string BadRequest = "Bad Request";
        public static string InvalidPayload = "Invalid payload";
    }

    public static class Profile
    {
        public static string UserNotFound = "User not found";
    }

    public static class Users
    {
        public static string UserNotFound = "User not found";
    }
}