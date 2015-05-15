varying vec3 wposition;
varying vec3 wnormal;
varying vec3 normal;
varying vec3 vertex_to_light_vector;
varying vec2 texture_coordinate;
varying vec4 color;
varying vec4 posL;
 
void main()
{
    // Defining The Material Colors
    vec4 AmbientColor = vec4(0.0, 0.0, 0.0, 0.0);
    vec3 DiffuseColor = color.xyz;//vec4(1.0, 0.0, 0.0, 1.0);
   //DiffuseColor=wnormal;

    // Scaling The Input Vector To Length 1
    vec3 normalized_normal = normalize(normal);
    vec3 normalized_vertex_to_light_vector = normalize(vertex_to_light_vector);
 
    // Calculating The Diffuse Term And Clamping It To [0;1]
    float DiffuseTerm = clamp(dot(normalized_normal, normalized_vertex_to_light_vector), 0.0, 1.0);
 
 DiffuseTerm=0.5*(1.0+cos(DiffuseTerm*20.0));
    // Calculating The Final Color
    gl_FragColor = AmbientColor + vec4(DiffuseColor * DiffuseTerm, color.w);
	//gl_FragColor = texture2D(my_color_texture, texture_coordinate);
}