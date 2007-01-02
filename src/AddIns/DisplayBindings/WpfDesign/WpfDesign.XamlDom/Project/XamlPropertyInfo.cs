﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace ICSharpCode.WpfDesign.XamlDom
{
	/// <summary>
	/// Represents a property assignable in XAML.
	/// This can be a normal .NET property or an attached property.
	/// </summary>
	internal abstract class XamlPropertyInfo
	{
		public abstract object GetValue(object instance);
		public abstract void SetValue(object instance, object value);
		public abstract void ResetValue(object instance);
		public abstract TypeConverter TypeConverter { get; }
		public abstract Type TargetType { get; }
		public abstract Type ReturnType { get; }
		public abstract string Name { get; }
		public abstract string FullyQualifiedName { get; }
		public abstract bool IsAttached { get; }
		public abstract bool IsCollection { get; }
		internal abstract void AddValue(object collectionInstance, XamlPropertyValue newElement);
	}
	
	internal class XamlDependencyPropertyInfo : XamlPropertyInfo
	{
		readonly DependencyProperty property;
		readonly bool isAttached;
		
		public XamlDependencyPropertyInfo(DependencyProperty property, bool isAttached)
		{
			Debug.Assert(property != null);
			this.property = property;
			this.isAttached = isAttached;
		}
		
		public override TypeConverter TypeConverter {
			get {
				return TypeDescriptor.GetConverter(this.ReturnType);
			}
		}
		
		public override string FullyQualifiedName {
			get {
				return this.TargetType.FullName + "." + this.Name;
			}
		}
		
		public override Type TargetType {
			get { return property.OwnerType; }
		}
		
		public override Type ReturnType {
			get { return property.PropertyType; }
		}
		
		public override string Name {
			get { return property.Name; }
		}
		
		public override bool IsAttached {
			get { return isAttached; }
		}
		
		public override bool IsCollection {
			get { return false; }
		}
		
		public override object GetValue(object instance)
		{
			return ((DependencyObject)instance).GetValue(property);
		}
		
		public override void SetValue(object instance, object value)
		{
			((DependencyObject)instance).SetValue(property, value);
		}
		
		public override void ResetValue(object instance)
		{
			((DependencyObject)instance).ClearValue(property);
		}
		
		internal override void AddValue(object collectionInstance, XamlPropertyValue newElement)
		{
			throw new NotSupportedException();
		}
	}
	
	internal sealed class XamlNormalPropertyInfo : XamlPropertyInfo
	{
		PropertyDescriptor _propertyDescriptor;
		
		public XamlNormalPropertyInfo(PropertyDescriptor propertyDescriptor)
		{
			this._propertyDescriptor = propertyDescriptor;
		}
		
		public override object GetValue(object instance)
		{
			return _propertyDescriptor.GetValue(instance);
		}
		
		public override void SetValue(object instance, object value)
		{
			_propertyDescriptor.SetValue(instance, value);
		}
		
		public override void ResetValue(object instance)
		{
			_propertyDescriptor.ResetValue(instance);
		}
		
		public override Type ReturnType {
			get { return _propertyDescriptor.PropertyType; }
		}
		
		public override Type TargetType {
			get { return _propertyDescriptor.ComponentType; }
		}
		
		public override TypeConverter TypeConverter {
			get {
				if (_propertyDescriptor.PropertyType == typeof(object))
					return null;
				else
					return _propertyDescriptor.Converter;
			}
		}
		
		public override string FullyQualifiedName {
			get {
				return _propertyDescriptor.ComponentType.FullName + "." + _propertyDescriptor.Name;
			}
		}
		
		public override string Name {
			get { return _propertyDescriptor.Name; }
		}
		
		public override bool IsAttached {
			get { return false; }
		}
		
		public override bool IsCollection {
			get {
				return CollectionSupport.IsCollectionType(_propertyDescriptor.PropertyType);
			}
		}
		
		internal override void AddValue(object collectionInstance, XamlPropertyValue newElement)
		{
			CollectionSupport.AddToCollection(_propertyDescriptor.PropertyType, collectionInstance, newElement);
		}
	}
	
	static class CollectionSupport
	{
		public static bool IsCollectionType(Type type)
		{
			return typeof(IList).IsAssignableFrom(type)
				|| typeof(IDictionary).IsAssignableFrom(type)
				|| type.IsArray
				|| typeof(IAddChild).IsAssignableFrom(type);
		}
		
		public static void AddToCollection(Type collectionType, object collectionInstance, XamlPropertyValue newElement)
		{
			IAddChild addChild = collectionInstance as IAddChild;
			if (addChild != null) {
				if (newElement is XamlTextValue) {
					addChild.AddText((string)newElement.GetValueFor(null));
				} else {
					addChild.AddChild(newElement.GetValueFor(null));
				}
			} else {
				collectionType.InvokeMember(
					"Add", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance,
					null, collectionInstance,
					new object[] { newElement.GetValueFor(null) },
					CultureInfo.InvariantCulture);
			}
		}
	}
}
