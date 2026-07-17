using UnityEngine;

public class PrefabSwapper : MonoBehaviour
{
     public PrefabList prefabList;
    public int selectedPrefabIndex = 0; // Index của prefab được chọn

    // Hàm để thay đổi prefab trong prefab cha
    public void ChangePrefab(string prefabName)
    {   
        
        // Tìm prefab theo tên
        var prefabInfo = prefabList.prefabList.Find(p => p.name == prefabName);
        if (prefabInfo.prefab != null)
        {
            // Thực hiện thay đổi prefab trong prefab cha (ví dụ)
            // Đây là một ví dụ đơn giản, bạn cần phải viết logic cụ thể cho từng trường hợp
            // Ví dụ: xóa prefab con hiện tại và thêm prefab mới vào
            DestroyImmediate(transform.GetChild(0).gameObject); // Xóa prefab con hiện tại
            Instantiate(prefabInfo.prefab, transform); // Thêm prefab mới vào
        }
        else
        {
            return;
            //Debug.LogError("Prefab not found: " + prefabName);
            
        }
    }
}
