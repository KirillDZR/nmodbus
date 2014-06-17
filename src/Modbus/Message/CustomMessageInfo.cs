﻿using System;
using System.Globalization;
using Modbus.Data;
using Unme.Common;

namespace Modbus.Message
{
	/// <summary>
	/// 
	/// </summary>
	public class CustomMessageInfo
	{
		private readonly Type _type;
		private readonly Func<IModbusMessage, DataStore, IModbusMessage> _applyRequest;
		private readonly Func<IModbusMessageRtu> _instanceGetter;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="applyRequest"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public CustomMessageInfo(Type type, Func<IModbusMessage, DataStore, IModbusMessage> applyRequest)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (applyRequest == null)
				throw new ArgumentNullException("applyRequest");

			_type = type;
			_applyRequest = applyRequest;

			// lazily initialize the instance, this will only be needed for the RTU protocol so 
			// we don't actually require the type argument to implement IModbusMessageRtu
			_instanceGetter = FunctionalUtility.Memoize(() =>
			{
				if (!typeof(IModbusMessageRtu).IsAssignableFrom(type))
				{
					throw new ArgumentException(
						String.Format(CultureInfo.InvariantCulture,
						"Custom type {0} needs to implement the {1} interface.",
						type.Name,
						typeof(IModbusMessageRtu).Name));
				}

				return (IModbusMessageRtu) Activator.CreateInstance(Type);
			});
		}

		/// <summary>
		/// 
		/// </summary>
		public Type Type
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Func<IModbusMessage, DataStore, IModbusMessage> ApplyRequest
		{
			get
			{
				return _applyRequest;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public IModbusMessageRtu Instance
		{
			get
			{
				return _instanceGetter.Invoke();
			}
		}
	}
}
