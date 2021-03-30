using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Caliburn.Micro
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Applies the action to each elemen in the list.
        /// </summary>
        /// <typeparam name="T">The enumerable item's type.</typeparam>
        /// <param name="enumerable">The elements to enumerate.</param>
        /// <param name="action">The action to apply to each item in the list.</param>
        public static void Apply<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action(item);
        }

        /// <summary>
        /// Gets all the attributes of a particular type.
        /// </summary>
        /// <typeparam name="T">The type of attributes to get.</typeparam>
        /// <param name="member">The member to inspect for attributes.</param>
        /// <param name="inherit">Whether or not to search for inherited attributes.</param>
        /// <returns>The list of attributes found.</returns>
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member, bool inherit)
        {
            return Attribute.GetCustomAttributes(member, inherit).OfType<T>();
        }

        /// <summary>
        /// Gets all the <see cref="FrameworkElement"/> instances with names in the scope.
        /// </summary>
        /// <returns>Names <see cref="FrameworkElement"/> instances in the provided scope.</returns>
        /// <remarks>Pass in a <see cref="DependencyObject"/> and receive a list of named <see cref="FrameworkElement"/> instance in the same scope.</remarks>
        public static Func<DependencyObject, IEnumerable<FrameworkElement>> GetNamedElementsInScope =
            elementInScope =>
            {
                var root = elementInScope;
                var previous = elementInScope;

                while (true)
                {
                    if (root == null)
                    {
                        root = previous;
                        break;
                    }

                    if (root is UserControl)
                        break;
                    if ((bool)root.GetValue(View.IsScopeRootProperty))
                        break;

                    previous = root;
                    root = VisualTreeHelper.GetParent(previous);
                }

                var descendants = new List<FrameworkElement>();
                var queue = new Queue<DependencyObject>();
                queue.Enqueue(root);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var currentElement = current as FrameworkElement;

                    if (currentElement != null && !string.IsNullOrEmpty(currentElement.Name))
                        descendants.Add(currentElement);

                    if (current is UserControl && current != root)
                        continue;

                    var childCount = VisualTreeHelper.GetChildrenCount(current);
                    if (childCount > 0)
                    {
                        for (var i = 0; i < childCount; i++)
                        {
                            var childDo = VisualTreeHelper.GetChild(current, i);
                            queue.Enqueue(childDo);
                        }
                    }
                    else if (current is ContentControl contentControl)
                    {
                        if (contentControl.Content is DependencyObject)
                            queue.Enqueue(contentControl.Content as DependencyObject);

                        if (contentControl is HeaderedContentControl headeredControl && headeredControl.Header is DependencyObject)
                            queue.Enqueue(headeredControl.Header as DependencyObject);
                    }
                    else if (current is ItemsControl itemsControl)
                    {
                        itemsControl.Items.OfType<DependencyObject>().Apply(queue.Enqueue);

                        if (itemsControl is HeaderedItemsControl headeredItemsControl && headeredItemsControl.Header is DependencyObject)
                            queue.Enqueue(headeredItemsControl.Header as DependencyObject);
                    }
                }

                return descendants;
            };

        /// <summary>
        /// Searches through the list of named elements looking for a case-insensitive match.
        /// </summary>
        /// <param name="elementsToSearch">The named elements to search through.</param>
        /// <param name="name">The name to search for.</param>
        /// <returns>The named element or null if not found.</returns>
        public static FrameworkElement FindName(this IEnumerable<FrameworkElement> elementsToSearch, string name)
        {
            return elementsToSearch.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Gets a property by name, ignoring case and searching all interfaces.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="propertyName">The property to search for.</param>
        /// <returns>The property or null if not found.</returns>
        public static PropertyInfo GetPropertyCaseInsensitive(this Type type, string propertyName)
        {
            var typeList = new List<Type> { type };

            if (type.IsInterface)
                typeList.AddRange(type.GetInterfaces());

            var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            if (ConventionManager.IncludeStaticProperties)
                flags = flags | BindingFlags.Static;

            return typeList
                .Select(interfaceType => interfaceType.GetProperty(propertyName, flags))
                .FirstOrDefault(property => property != null);
        }

        /// <summary>
        /// Converts an expression into a <see cref="MemberInfo"/>
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <returns>The member info.</returns>
        public static MemberInfo GetMemberInfo(this System.Linq.Expressions.Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression unaryExpression)
            {
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }

            else memberExpression = (MemberExpression)lambda.Body;

            return memberExpression.Member;
        }
    }
}