using System;
using UnityEngine;

namespace TFW.AStar
{
    public class Singleton<T> where T : new()
    {
        // 单件实例对象
        protected static T mInstance = default;

        /// <summary>
        /// 获取单件对象
        /// </summary>
        /// <value>单件实例</value>
        public static T Instance
        {
            get
            {
                // 没有单件，则立即创建一个
                // Thread Unsafe
                if (mInstance == null)
                    mInstance = ((default(T) == null) ? Activator.CreateInstance<T>() : default);

                return mInstance;
            }
        }

        /// <summary>
        /// 清理单件对象
        /// </summary>
        public void CleanInstance()
        {
            mInstance = default;
        }
    }
}
