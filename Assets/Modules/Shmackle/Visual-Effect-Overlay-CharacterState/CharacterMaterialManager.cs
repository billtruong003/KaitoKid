// Đặt tại: Assets/_Shmackle/Scripts/Player/
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using _Shmackle.Scripts.Player;

public class CharacterMaterialManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu đến ShmackleNetworkRig.")]
    [SerializeField]
    private ShmackleNetworkRig shmackleNetworkRig;
    
    [Header("Dependencies")]
    [Tooltip("Tham chiếu đến CharacterPartController để truy vấn trạng thái trang bị.")]
    [SerializeField]
    private CharacterPartController characterPartController;

    [SerializeField]
    private Material _defaultBodyMaterial;
    [SerializeField]
    private Material _defaultBodyGertMaterial;

    private static readonly int GertStateShaderID = Shader.PropertyToID("_GertState");

    /// <summary>
    /// Áp dụng material mặc định cho tất cả các bộ phận đang được trang bị.
    /// </summary>
    public void ApplyDefaultMaterials()
    {
        ApplyMaterialsBasedOnSelector((partType, runtime) =>
                                      {
                                          if (runtime == null)
                                          {
                                              if (partType == CharacterPartType.Body)
                                              {
                                                  return _defaultBodyMaterial;
                                              }

                                              return null;
                                          }

                                          if (shmackleNetworkRig != null 
                                              && shmackleNetworkRig.isDripTransformation
                                              && runtime.DripDataTransformation != null)
                                          {
                                              return runtime.DripDataTransformation.TransformPartMaterial;
                                          }

                                          return runtime.CharacterPartMaterial;
                                      });
    }

    /// <summary>
    /// Áp dụng material GERT cho tất cả các bộ phận đang được trang bị.
    /// </summary>
    public void ApplyGertMaterials()
    {
        ApplyMaterialsBasedOnSelector((partType, runtime) =>
                                                 {
                                                     if (runtime == null)
                                                     {
                                                         if (partType == CharacterPartType.Body)
                                                         {
                                                             return _defaultBodyGertMaterial;
                                                         }

                                                         return null;
                                                     }

                                                     if (shmackleNetworkRig != null 
                                                         && shmackleNetworkRig.isDripTransformation
                                                         && runtime.DripDataTransformation != null)
                                                     {
                                                         return runtime.DripDataTransformation.TransformGertMaterial ?? runtime.DripDataTransformation.TransformPartMaterial;
                                                     }
                                                     
                                                     return runtime.CharacterGertMaterial ??
                                                            runtime.CharacterPartMaterial;
                                                 });
    }

    /// <summary>
    /// Đặt giá trị cho thuộc tính shader _GertState trên tất cả các material đang được áp dụng.
    /// </summary>
    public void SetGertShaderProperty(float value)
    {
        if (characterPartController == null) return;

        // Lấy danh sách các renderer đang active và áp dụng thuộc tính
        foreach (var renderer in GetActiveRenderers())
        {
            var material = renderer.material; // Dùng .material để không sửa shared asset
            if (material != null && material.HasProperty(GertStateShaderID))
            {
                material.SetFloat(GertStateShaderID, value);
            }
        }
    }

    /// <summary>
    /// Hàm private để tái sử dụng logic. Nó sẽ tự lấy trạng thái mới nhất.
    /// </summary>
    private void ApplyMaterialsBasedOnSelector(System.Func<CharacterPartType, DripData_Runtime, Material> selector)
    {
        if (characterPartController == null) return;

        // "Kéo" thông tin mới nhất từ CharacterPartController mỗi khi được gọi
        foreach (var part in characterPartController.characterPartMap.Values)
        {
            // Chỉ xử lý các bộ phận đang có drip data (đang được trang bị)
            if (part.SkinnedMeshRenderer != null)
            {
                var materialToApply = selector(part.PartType, part.DripData);
                if (materialToApply != null)
                {
                    part.SkinnedMeshRenderer.material = materialToApply;
                }
            }
        }
    }

    /// <summary>
    /// Lấy danh sách các SkinnedMeshRenderer đang được kích hoạt (có DripData).
    /// </summary>
    private IEnumerable<SkinnedMeshRenderer> GetActiveRenderers()
    {
        if (characterPartController == null) yield break;

        foreach (var part in characterPartController.characterPartMap.Values)
        {
            if (part.SkinnedMeshRenderer != null)
            {
                yield return part.SkinnedMeshRenderer;
            }
        }
    }
}