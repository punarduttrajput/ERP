namespace ERP.Admissions.Domain;

public enum ApplicationState
{
    Draft = 0,
    Submitted = 1,
    UnderVerification = 2,
    Verified = 3,
    MeritEvaluated = 4,
    OfferMade = 5,
    OfferAccepted = 6,
    Enrolled = 7,
    Rejected = 8,
    Withdrawn = 9
}
