﻿using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SwingingDoor
{
    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        private MeshRef meshRef;
        public Matrixf ModelMat = new Matrixf();
        public LoadCustomModels models { get => capi.ModLoader.GetModSystem<LoadCustomModels>(); }
        public bool shouldRender;
        AssetLocation location;
        Vec3f rotation;
        Vec3f scale;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, AssetLocation location, Vec3f rotation, Vec3f scale, out bool failed)
        {
            failed = false;
            try
            {
                this.capi = capi;
                this.pos = pos;
                this.rotation = rotation;
                this.location = location;
                this.scale = scale;
                MeshData mesh = models.meshes[location];
                if (models.gltfTextures.TryGetValue(location, out TextureAtlasPosition tPos))
                {
                    mesh = mesh.WithTexPos(tPos);
                }
                meshRef = capi.Render.UploadMesh(mesh);
            }
            catch (Exception)
            {
                failed = true;
                Dispose();
            }
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            meshRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null) return;
            IRenderAPI render = capi.Render;
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            render.GlToggleBlend(true, EnumBlendMode.Standard);
            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            if (models.gltfTextures.TryGetValue(location, out TextureAtlasPosition tex))
            {
                prog.Tex2D = tex.atlasTextureId;
            }
            else prog.Tex2D = 0;

            prog.ModelMatrix = ModelMat.Identity()
                .Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
                .Translate(0.5, 0.5, 0.5)
                .RotateDeg(rotation)
                .Scale(scale.X, scale.Y, scale.Z)
                .Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;

            capi.Render.RenderMesh(meshRef);
            prog.Stop();
        }
    }
}