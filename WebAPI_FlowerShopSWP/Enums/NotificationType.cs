namespace WebAPI_FlowerShopSWP.Enums
{
    public class NotificationType
    {
        public const string OrderCreated = "ORDER_CREATED";
        public const string OrderConfirmed = "ORDER_CONFIRMED";
        public const string OrderShipping = "ORDER_SHIPPING";
        public const string OrderCompleted = "ORDER_COMPLETED";
        public const string OrderCancelled = "ORDER_CANCELLED";

        public const string ReviewReceived = "REVIEW_RECEIVED";

        public const string SellerApproved = "SELLER_APPROVED";
        public const string SellerRejected = "SELLER_REJECTED";

        public const string FlowerOutOfStock = "FLOWER_OUT_OF_STOCK";

        public const string NewFollower = "NEW_FOLLOWER";
        public const string SellerNewProduct = "SELLER_NEW_PRODUCT";

        public const string NewMessage = "NEW_MESSAGE";
    }

    public static class RelatedType
    {
        public const string Order = "order";
        public const string Review = "review";
        public const string Flower = "flower";
        public const string Seller = "seller";
        public const string User = "user";
        public const string Chat = "chat";
        public const string SellerRegistration = "seller_registration";
    }
}
