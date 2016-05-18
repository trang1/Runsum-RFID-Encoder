using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RfidEncoder.ViewModels
{
    // this code helps our classes to implement system interface INotifyPropertyChanged
    public interface INotifyPropertyChanged : System.ComponentModel.INotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }

    public static class ExtensionMethods
    {
        public static void OnPropertyChanged<T>(this INotifyPropertyChanged notifyPropertyChanged,
            Expression<Func<T>> memberExpression)
        {
            notifyPropertyChanged.OnPropertyChanged(memberExpression.GetMemberInfo().Name);
        }

        public static MemberInfo GetMemberInfo<T>(this Expression<Func<T>> memberExpression)
        {
            return ((MemberExpression)memberExpression.Body).Member;
        }
    }
}
