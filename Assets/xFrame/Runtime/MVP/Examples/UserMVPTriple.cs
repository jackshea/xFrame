namespace xFrame.MVP.Examples
{
    /// <summary>
    /// 用户MVP三元组
    /// </summary>
    public class UserMVPTriple : MVPTriple<UserModel, UserView, UserPresenter>
    {
        public UserMVPTriple(UserModel model, UserView view, UserPresenter presenter) 
            : base(model, view, presenter)
        {
        }
    }
}
