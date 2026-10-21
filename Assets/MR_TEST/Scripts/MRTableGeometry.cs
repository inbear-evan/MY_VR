using UnityEngine;
using Meta.XR.MRUtilityKit;

namespace Session2MR
{
    public struct TablePose
    {
        public Vector3 center;
        public Quaternion rotation;
        public Vector2 size;
        public string source;
        public bool Valid => size.x >= .35f && size.y >= .25f && size.x <= 5 && size.y <= 5;
    }
    public static class MRTableGeometry
    {
        public static bool FromAnchor(MRUKAnchor anchor, out TablePose pose)
        {
            pose=default;
            if(!anchor || (anchor.Label & MRUKAnchor.SceneLabels.TABLE)==0)return false;
            if(anchor.PlaneRect.HasValue && FromPlane(anchor.transform.localToWorldMatrix,anchor.PlaneRect.Value,out pose))
            {pose.source="Quest 공간 정보 · TABLE 평면";return true;}
            if(anchor.VolumeBounds.HasValue && FromVolume(anchor.transform.localToWorldMatrix,anchor.VolumeBounds.Value,out pose))
            {pose.source="Quest 공간 정보 · TABLE 부피";return true;}
            return false;
        }
        public static bool FromPlane(Matrix4x4 matrix, Rect rect, out TablePose pose)
        {
            pose=default;
            var right=matrix.MultiplyVector(Vector3.right);
            var depth=matrix.MultiplyVector(Vector3.up);
            var normal=Vector3.Cross(right,depth).normalized;
            if(Mathf.Abs(Vector3.Dot(normal,Vector3.up))<.85f)return false;
            if(Vector3.Dot(normal,Vector3.up)<0)normal=-normal;
            pose=new TablePose {center=matrix.MultiplyPoint3x4(new Vector3(rect.center.x,rect.center.y,0)),
                rotation=Quaternion.LookRotation(Vector3.Cross(right.normalized,normal),normal),
                size=new Vector2(rect.width*right.magnitude,rect.height*depth.magnitude)};
            return pose.Valid;
        }
        public static bool FromVolume(Matrix4x4 matrix, Bounds bounds, out TablePose pose)
        {
            pose=default;
            var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};
            int upAxis=0;float best=0;
            for(int i=0;i<3;i++){float dot=Mathf.Abs(Vector3.Dot(matrix.MultiplyVector(axes[i]).normalized,Vector3.up));if(dot>best){best=dot;upAxis=i;}}
            if(best<.85f)return false;
            int widthAxis=upAxis==0?1:0;int depthAxis=3-upAxis-widthAxis;
            var local=bounds.center;
            local[upAxis]+=bounds.extents[upAxis]*Mathf.Sign(Vector3.Dot(matrix.MultiplyVector(axes[upAxis]),Vector3.up));
            var right=matrix.MultiplyVector(axes[widthAxis]);var normal=matrix.MultiplyVector(axes[upAxis]).normalized;
            if(Vector3.Dot(normal,Vector3.up)<0)normal=-normal;
            pose=new TablePose {center=matrix.MultiplyPoint3x4(local),rotation=Quaternion.LookRotation(Vector3.Cross(right.normalized,normal),normal),
                size=new Vector2(bounds.size[widthAxis]*right.magnitude,bounds.size[depthAxis]*matrix.MultiplyVector(axes[depthAxis]).magnitude)};
            return pose.Valid;
        }
    }
}
