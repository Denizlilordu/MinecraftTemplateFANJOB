using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace VoxelGame;

public class Shader
{
    public int Handle;

    public Shader(string vertexCode, string fragmentCode)
    {
        int vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vertexCode);
        GL.CompileShader(vertex);
        CheckShader(vertex);

        int fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fragmentCode);
        GL.CompileShader(fragment);
        CheckShader(fragment);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex);
        GL.AttachShader(Handle, fragment);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetProgramInfoLog(Handle));

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    void CheckShader(int shader)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetShaderInfoLog(shader));
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetMatrix4(string name, Matrix4 mat)
    {
        int loc = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(loc, false, ref mat);
    }

    public void SetInt(string name, int value)
    {
        int loc = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(loc, value);
    }

    public void SetFloat(string name, float value)
    {
        int loc = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(loc, value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        int loc = GL.GetUniformLocation(Handle, name);
        GL.Uniform3(loc, value.X, value.Y, value.Z);
    }
}