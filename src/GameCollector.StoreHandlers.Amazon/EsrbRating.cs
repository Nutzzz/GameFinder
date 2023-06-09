namespace GameCollector.StoreHandlers.Amazon;

public enum EsrbRating
{
    NO_RATING = -1,
    everyone = 1,
    everyone_10_plus,
    teen,
    mature,
    adults_only,    // TODO: Confirm this; this name is a guess
    rating_pending,
}
