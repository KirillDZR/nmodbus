using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Modbus.Unme.Common;

namespace Modbus.Data
{
	/// <summary>
	/// Object simulation of device memory map.
	/// The underlying collections are thread safe when using the ModbusMaster API to read/write values.
	/// You can use the SyncRoot property to synchronize direct access to the DataStore collections.
	/// </summary>
	public class DataStore
	{
		private readonly object _syncRoot = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="DataStore"/> class.
		/// </summary>
		public DataStore()
		{
			CoilDiscretes = new ModbusDataCollection<bool> { ModbusDataType = ModbusDataType.Coil };
			InputDiscretes = new ModbusDataCollection<bool> { ModbusDataType = ModbusDataType.Input };
			HoldingRegisters = new ModbusDataCollection<ushort> { ModbusDataType = ModbusDataType.HoldingRegister };
			InputRegisters = new ModbusDataCollection<ushort> { ModbusDataType = ModbusDataType.InputRegister };
		}

		/// <summary>
		/// Occurs when the DataStore is written to via a Modbus command.
		/// </summary>		
		public event EventHandler<DataStoreEventArgs> DataStoreWrittenTo;

		/// <summary>
		/// Occurs when the DataStore is read from via a Modbus command.
		/// </summary>
		public event EventHandler<DataStoreEventArgs> DataStoreReadFrom;

		/// <summary>
		/// Gets the coil discretes.
		/// </summary>
		public ModbusDataCollection<bool> CoilDiscretes { get; private set; }

		/// <summary>
		/// Gets the input discretes.
		/// </summary>
		public ModbusDataCollection<bool> InputDiscretes { get; private set; }

		/// <summary>
		/// Gets the holding registers.
		/// </summary>
		public ModbusDataCollection<ushort> HoldingRegisters { get; private set; }

		/// <summary>
		/// Gets the input registers.
		/// </summary>
		public ModbusDataCollection<ushort> InputRegisters { get; private set; }

		/// <summary>
		/// An object that can be used to synchronize direct access to the DataStore collections.
		/// </summary>
		public object SyncRoot
		{
			get
			{
				return _syncRoot;
			}
		}

        /// <summary>
        /// Retrieves subset of data from collection.
        /// </summary>
	    /// <param name="dataStore"></param>
	    /// <param name="dataSource"></param>
	    /// <param name="startAddress"></param>
	    /// <param name="count"></param>
	    /// <param name="syncRoot"></param>
        /// <param name="sendEvent"></param>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <typeparam name="U">The type of elements in the collection.</typeparam>
	    /// <returns></returns>
	    /// <exception cref="ArgumentOutOfRangeException"></exception>
	    public static T ReadData<T, U>(DataStore dataStore, ModbusDataCollection<U> dataSource, ushort startAddress,
	        ushort count, object syncRoot, bool sendEvent = true) where T : Collection<U>, new()
	    {
            int startIndex = startAddress + 1;

            // Start address was out of range. Must be non-negative and <= the size of the collection.
	        if (startIndex >= dataSource.Count)
	            throw new ArgumentOutOfRangeException("startAddress");

            // Read is outside valid range.
			if (dataSource.Count < startIndex + count)
				throw new ArgumentOutOfRangeException("startAddress");

			U[] dataToRetrieve;
			lock (syncRoot)
				dataToRetrieve = dataSource.Slice(startIndex, count).ToArray();

			var result = new T();
			for (var i = 0; i < count; i++)
				result.Add(dataToRetrieve[i]);

		    if (sendEvent)
		    {
		        dataStore.DataStoreReadFrom.Raise(dataStore,
		            DataStoreEventArgs.CreateDataStoreEventArgs(startAddress, dataSource.ModbusDataType, result));
		    }

		    return result;
		}

        /// <summary>
        /// Write data to data store.
        /// </summary>
	    /// <param name="dataStore"></param>
	    /// <param name="items"></param>
	    /// <param name="destination"></param>
	    /// <param name="startAddress"></param>
	    /// <param name="syncRoot"></param>
        /// <param name="sendEvent"></param>
        /// <typeparam name="TData">The type of the data.</typeparam>
	    /// <exception cref="ArgumentOutOfRangeException"></exception>
	    public static void WriteData<TData>(DataStore dataStore, IEnumerable<TData> items,
	        ModbusDataCollection<TData> destination, ushort startAddress, object syncRoot, bool sendEvent = true)
	    {
            var startIndex = startAddress + 1;

            // Start address was out of range. Must be non-negative and <= the size of the collection.
	        if (startIndex >= destination.Count)
	            throw new ArgumentOutOfRangeException("startAddress");

            // Items collection is too large to write at specified start index.
	        if (destination.Count < startIndex + items.Count())
	            throw new ArgumentOutOfRangeException("startAddress");

	        lock (syncRoot)
	            Update(items, destination, startIndex);

	        if (sendEvent)
	        {
	            dataStore.DataStoreWrittenTo.Raise(dataStore,
	                DataStoreEventArgs.CreateDataStoreEventArgs(startAddress, destination.ModbusDataType, items));
	        }
	    }

	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="dataStore"></param>
	    /// <param name="dataSource"></param>
	    /// <param name="index"></param>
	    /// <typeparam name="TData"></typeparam>
	    /// <returns></returns>
	    public static TData Read<TData>(DataStore dataStore, ModbusDataCollection<TData> dataSource, ushort index)
	    {
            lock (dataStore.SyncRoot)
	        {
                return dataSource[index + 1];
	        }
	    }

	    /// <summary>
	    /// 
	    /// </summary>
        /// <param name="dataSource"></param>
	    /// <param name="index"></param>
	    /// <param name="item"></param>
	    /// <param name="dataStore"></param>
	    /// <typeparam name="TData"></typeparam>
	    /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void Write<TData>(DataStore dataStore, ModbusDataCollection<TData> dataSource, int index, TData item)
        {
            lock (dataStore.SyncRoot)
            {
                dataSource[index + 1] = item;
            }
        }

        /// <summary>
        /// Updates subset of values in a collection.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="destination"></param>
        /// <param name="startIndex"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void Update<T>(IEnumerable<T> items, IList<T> destination, int startIndex)
		{
            // Index was out of range. Must be non-negative and less than the size of the collection.
			if (startIndex < 0 || destination.Count < startIndex + items.Count())
				throw new ArgumentOutOfRangeException("startIndex");

			items.ForEachWithIndex((item, index) => destination[index + startIndex] = item);
		}
	}
}
