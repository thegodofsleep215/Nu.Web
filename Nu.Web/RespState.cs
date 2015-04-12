namespace Nu.Web
{
    enum RespState
    {
        Ok = 200,
        Created = 201,
        Accepted = 202,
        NoContent=204,

        MovedPermanetly = 301,
        Reidrection = 302,
        NotModified = 304,

        BadRequest = 400,
        Unauthroized = 401,
        Forbidden = 403,
        NotFound = 404,

        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503
    }
}