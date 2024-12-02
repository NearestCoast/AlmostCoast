using System.Collections.Generic;
using UnityEngine;

namespace _Project.Inventories.Items
{
    [CreateAssetMenu(fileName = "ItemDataMaster", menuName = "Project/ItemDataMaster", order = 0)]
    public class ItemDataMaster : ScriptableObject
    {
        [SerializeField] private List<KeyData> keyDatas;
        public List<KeyData> KeyDatas => keyDatas;
    }
}