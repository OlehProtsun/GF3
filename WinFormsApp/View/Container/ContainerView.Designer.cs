using Guna.UI2.WinForms;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp.View.Container.Helpers;

namespace WinFormsApp.View.Container
{
    partial class ContainerView
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _gridVPen.Dispose();
                _gridHPen.Dispose();
                _conflictPen.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges213 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges214 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges207 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(ContainerView));
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges208 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges209 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges210 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges211 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges212 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle21 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle22 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle23 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle24 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges227 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges228 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges215 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges216 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges217 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges218 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges219 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges220 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges221 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges222 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges223 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges224 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges225 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges226 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges233 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges234 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges229 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges230 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges231 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges232 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges255 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges256 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges245 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges246 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges235 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges236 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges237 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges238 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges239 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges240 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges241 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges242 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges243 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges244 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges247 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges248 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges249 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges250 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle25 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle26 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle27 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle28 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges251 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges252 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges253 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges254 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges267 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges268 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges257 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges258 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges259 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges260 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges261 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges262 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges263 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges264 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges265 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges266 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges273 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges274 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges269 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges270 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges271 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges272 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges291 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges292 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges275 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges276 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges277 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges278 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges279 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges280 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges281 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges282 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges283 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges284 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges285 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges286 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges287 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges288 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges289 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges290 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges299 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges300 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges293 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges294 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges295 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges296 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges297 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges298 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges351 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges352 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges309 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges310 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges301 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges302 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges303 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges304 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges305 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges306 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges307 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges308 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges319 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges320 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges311 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges312 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges313 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges314 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges315 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges316 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges317 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges318 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges321 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges322 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges323 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges324 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges325 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges326 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges327 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges328 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges329 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges330 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges331 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges332 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges333 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges334 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges335 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges336 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges337 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges338 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges339 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges340 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges341 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges342 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges343 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges344 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges345 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges346 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges347 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges348 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges349 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges350 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges361 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges362 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges355 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges356 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges357 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges358 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges359 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges360 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges371 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges372 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges363 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges364 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges365 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges366 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges367 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges368 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges369 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges370 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle29 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle30 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle31 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle32 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges383 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges384 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges377 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges378 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges379 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges380 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges381 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges382 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle33 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle34 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle35 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle36 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges395 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges396 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges385 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges386 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges387 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges388 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges389 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges390 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges391 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges392 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges393 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges394 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges405 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges406 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges397 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges398 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges399 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges400 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges401 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges402 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            DataGridViewCellStyle dataGridViewCellStyle37 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle38 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle39 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle40 = new DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges403 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges404 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges411 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges412 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges407 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges408 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges409 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges410 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges353 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges354 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges375 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges376 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges373 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges374 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            tabControl = new Guna2TabControl();
            tabList = new TabPage();
            guna2GroupBox1 = new Guna2GroupBox();
            label6 = new Label();
            btnSearch = new Guna2Button();
            btnAdd = new Guna2Button();
            inputSearch = new Guna2TextBox();
            label7 = new Label();
            containerGrid = new Guna2DataGridView();
            tabEdit = new TabPage();
            guna2GroupBox3 = new Guna2GroupBox();
            inputContainerName = new Guna2TextBox();
            numberContainerId = new Guna2NumericUpDown();
            btnSave = new Guna2Button();
            btnCancel = new Guna2Button();
            inputContainerNote = new Guna2TextBox();
            label24 = new Label();
            label3 = new Label();
            guna2Button3 = new Guna2Button();
            label25 = new Label();
            guna2GroupBox2 = new Guna2GroupBox();
            btnBackToContainerList = new Guna2Button();
            label26 = new Label();
            guna2Button4 = new Guna2Button();
            label27 = new Label();
            tabProfile = new TabPage();
            guna2GroupBox8 = new Guna2GroupBox();
            guna2GroupBox4 = new Guna2GroupBox();
            guna2Button8 = new Guna2Button();
            guna2Button2 = new Guna2Button();
            label5 = new Label();
            btnScheduleSearch = new Guna2Button();
            btnScheduleAdd = new Guna2Button();
            inputScheduleSearch = new Guna2TextBox();
            btnCancelProfile2 = new Guna2Button();
            guna2Button5 = new Guna2Button();
            scheduleGrid = new Guna2DataGridView();
            guna2Button7 = new Guna2Button();
            guna2Button1 = new Guna2Button();
            guna2GroupBox6 = new Guna2GroupBox();
            lblContainerNote = new Guna2TextBox();
            label2 = new Label();
            btnCancelProfile = new Guna2Button();
            btnEdit = new Guna2Button();
            btnDelete = new Guna2Button();
            labelId = new Label();
            label8 = new Label();
            label1 = new Label();
            guna2Button9 = new Guna2Button();
            lblContainerName = new Label();
            guna2GroupBox7 = new Guna2GroupBox();
            btnBackToContainerListFromProfile = new Guna2Button();
            label22 = new Label();
            guna2Button10 = new Guna2Button();
            label23 = new Label();
            tabScheduleEdit = new TabPage();
            panel3 = new NoFocusScrollPanel();
            guna2GroupBox19 = new Guna2GroupBox();
            label37 = new Label();
            guna2Button30 = new Guna2Button();
            btnSearchEmployeeInAvailabilityEdit = new Guna2Button();
            textBoxSearchValue3FromScheduleEdit = new Guna2TextBox();
            btnRemoveEmployeeFromGroup = new Guna2Button();
            lblEmployeeId = new Label();
            comboboxEmployee = new Guna2ComboBox();
            label29 = new Label();
            btnAddEmployeeToGroup = new Guna2Button();
            label36 = new Label();
            btnShowHideEmployee = new Guna2Button();
            guna2Button28 = new Guna2Button();
            guna2GroupBox16 = new Guna2GroupBox();
            btnShowHideNote = new Guna2Button();
            inputScheduleNote = new Guna2TextBox();
            guna2Button29 = new Guna2Button();
            guna2GroupBox5 = new Guna2GroupBox();
            guna2GroupBox18 = new Guna2GroupBox();
            comboScheduleAvailability = new Guna2ComboBox();
            lblAvailabilityID = new Label();
            btnSearchAvailabilityFromScheduleEdit = new Guna2Button();
            label39 = new Label();
            guna2Button26 = new Guna2Button();
            textBoxSearchValue2FromScheduleEdit = new Guna2TextBox();
            label15 = new Label();
            guna2GroupBox17 = new Guna2GroupBox();
            guna2Button23 = new Guna2Button();
            comboScheduleShop = new Guna2ComboBox();
            textBoxSearchValueFromScheduleEdit = new Guna2TextBox();
            btnSearchShopFromScheduleEdit = new Guna2Button();
            labelScheduleShop = new Label();
            label21 = new Label();
            lbShopId = new Label();
            btnShowHideInfo = new Guna2Button();
            btnScheduleCancel = new Guna2Button();
            label14 = new Label();
            inputScheduleName = new Guna2TextBox();
            label31 = new Label();
            btnGenerate = new Guna2Button();
            label13 = new Label();
            inputMaxFull = new Guna2NumericUpDown();
            label12 = new Label();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            label4 = new Label();
            inputShift2 = new Guna2TextBox();
            label28 = new Label();
            label30 = new Label();
            inputShift1 = new Guna2TextBox();
            guna2Button6 = new Guna2Button();
            inputMaxConsecutiveFull = new Guna2NumericUpDown();
            label32 = new Label();
            numberScheduleId = new Guna2NumericUpDown();
            inputMaxConsecutiveDays = new Guna2NumericUpDown();
            inputPeoplePerShift = new Guna2NumericUpDown();
            inputMonth = new Guna2NumericUpDown();
            inputYear = new Guna2NumericUpDown();
            inputMaxHours = new Guna2NumericUpDown();
            panel2 = new Panel();
            guna2GroupBox9 = new Guna2GroupBox();
            btnBackToScheduleList = new Guna2Button();
            label33 = new Label();
            guna2Button11 = new Guna2Button();
            btnScheduleSave = new Guna2Button();
            label34 = new Label();
            panel1 = new Panel();
            button1 = new Button();
            guna2GroupBox15 = new Guna2GroupBox();
            guna2Button19 = new Guna2Button();
            guna2Button21 = new Guna2Button();
            guna2Button22 = new Guna2Button();
            guna2Button25 = new Guna2Button();
            dataGridAvailabilityOnScheduleEdit = new Guna2DataGridView();
            guna2GroupBox11 = new Guna2GroupBox();
            guna2Button12 = new Guna2Button();
            guna2Button13 = new Guna2Button();
            guna2Button14 = new Guna2Button();
            slotGrid = new Guna2DataGridView();
            tabScheduleProfile = new TabPage();
            guna2GroupBox14 = new Guna2GroupBox();
            lblScheduleNote = new Guna2TextBox();
            lblScheduleMonth = new Label();
            label20 = new Label();
            labelScheduleNoteTitle = new Label();
            lblScheduleYear = new Label();
            btnScheduleDelete = new Guna2Button();
            lblScheduleFromContainer = new Label();
            btnScheduleEdit = new Guna2Button();
            label18 = new Label();
            btnScheduleProfileCancel = new Guna2Button();
            label19 = new Label();
            lblScheduleId = new Label();
            lbl12 = new Label();
            label35 = new Label();
            guna2Button24 = new Guna2Button();
            labelName = new Label();
            guna2GroupBox12 = new Guna2GroupBox();
            guna2Button15 = new Guna2Button();
            guna2Button16 = new Guna2Button();
            guna2Button17 = new Guna2Button();
            scheduleSlotProfileGrid = new Guna2DataGridView();
            guna2Button18 = new Guna2Button();
            guna2GroupBox13 = new Guna2GroupBox();
            btnBackToContainerProfileFromSheduleProfile = new Guna2Button();
            label16 = new Label();
            guna2Button20 = new Guna2Button();
            label17 = new Label();
            errorProviderContainer = new ErrorProvider(components);
            errorProviderSchedule = new ErrorProvider(components);
            guna2Elipse1 = new Guna2Elipse(components);
            guna2Elipse2 = new Guna2Elipse(components);
            guna2Elipse3 = new Guna2Elipse(components);
            guna2Elipse4 = new Guna2Elipse(components);
            guna2Elipse5 = new Guna2Elipse(components);
            btnAddNewSchedule = new Guna2Button();
            btnHideShowScheduleTable = new Guna2Button();
            this.btnCloseScheduleTable = new Guna2Button();
            tabControl.SuspendLayout();
            tabList.SuspendLayout();
            guna2GroupBox1.SuspendLayout();
            ((ISupportInitialize)containerGrid).BeginInit();
            tabEdit.SuspendLayout();
            guna2GroupBox3.SuspendLayout();
            ((ISupportInitialize)numberContainerId).BeginInit();
            guna2GroupBox2.SuspendLayout();
            tabProfile.SuspendLayout();
            guna2GroupBox8.SuspendLayout();
            guna2GroupBox4.SuspendLayout();
            ((ISupportInitialize)scheduleGrid).BeginInit();
            guna2GroupBox6.SuspendLayout();
            guna2GroupBox7.SuspendLayout();
            tabScheduleEdit.SuspendLayout();
            panel3.SuspendLayout();
            guna2GroupBox19.SuspendLayout();
            guna2GroupBox16.SuspendLayout();
            guna2GroupBox5.SuspendLayout();
            guna2GroupBox18.SuspendLayout();
            guna2GroupBox17.SuspendLayout();
            ((ISupportInitialize)inputMaxFull).BeginInit();
            ((ISupportInitialize)inputMaxConsecutiveFull).BeginInit();
            ((ISupportInitialize)numberScheduleId).BeginInit();
            ((ISupportInitialize)inputMaxConsecutiveDays).BeginInit();
            ((ISupportInitialize)inputPeoplePerShift).BeginInit();
            ((ISupportInitialize)inputMonth).BeginInit();
            ((ISupportInitialize)inputYear).BeginInit();
            ((ISupportInitialize)inputMaxHours).BeginInit();
            panel2.SuspendLayout();
            guna2GroupBox9.SuspendLayout();
            panel1.SuspendLayout();
            guna2GroupBox15.SuspendLayout();
            ((ISupportInitialize)dataGridAvailabilityOnScheduleEdit).BeginInit();
            guna2GroupBox11.SuspendLayout();
            ((ISupportInitialize)slotGrid).BeginInit();
            tabScheduleProfile.SuspendLayout();
            guna2GroupBox14.SuspendLayout();
            guna2GroupBox12.SuspendLayout();
            ((ISupportInitialize)scheduleSlotProfileGrid).BeginInit();
            guna2GroupBox13.SuspendLayout();
            ((ISupportInitialize)errorProviderContainer).BeginInit();
            ((ISupportInitialize)errorProviderSchedule).BeginInit();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabList);
            tabControl.Controls.Add(tabEdit);
            tabControl.Controls.Add(tabProfile);
            tabControl.Controls.Add(tabScheduleEdit);
            tabControl.Controls.Add(tabScheduleProfile);
            tabControl.Dock = DockStyle.Fill;
            tabControl.ItemSize = new Size(180, 40);
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1151, 1100);
            tabControl.TabButtonHoverState.BorderColor = Color.Empty;
            tabControl.TabButtonHoverState.FillColor = Color.FromArgb(40, 52, 70);
            tabControl.TabButtonHoverState.Font = new Font("Segoe UI Semibold", 10F);
            tabControl.TabButtonHoverState.ForeColor = Color.White;
            tabControl.TabButtonHoverState.InnerColor = Color.FromArgb(40, 52, 70);
            tabControl.TabButtonIdleState.BorderColor = Color.Empty;
            tabControl.TabButtonIdleState.FillColor = Color.FromArgb(33, 42, 57);
            tabControl.TabButtonIdleState.Font = new Font("Segoe UI Semibold", 10F);
            tabControl.TabButtonIdleState.ForeColor = Color.FromArgb(156, 160, 167);
            tabControl.TabButtonIdleState.InnerColor = Color.FromArgb(33, 42, 57);
            tabControl.TabButtonSelectedState.BorderColor = Color.Empty;
            tabControl.TabButtonSelectedState.FillColor = Color.FromArgb(76, 132, 255);
            tabControl.TabButtonSelectedState.Font = new Font("Segoe UI Semibold", 10F);
            tabControl.TabButtonSelectedState.ForeColor = Color.White;
            tabControl.TabButtonSelectedState.InnerColor = Color.FromArgb(76, 132, 255);
            tabControl.TabButtonSize = new Size(180, 40);
            tabControl.TabIndex = 0;
            tabControl.TabMenuBackColor = Color.FromArgb(33, 42, 57);
            tabControl.TabMenuOrientation = TabMenuOrientation.HorizontalTop;
            // 
            // tabList
            // 
            tabList.Controls.Add(guna2GroupBox1);
            tabList.Controls.Add(containerGrid);
            tabList.Location = new Point(4, 44);
            tabList.Name = "tabList";
            tabList.Padding = new Padding(3);
            tabList.Size = new Size(1143, 1052);
            tabList.TabIndex = 0;
            tabList.Text = "Containers";
            tabList.UseVisualStyleBackColor = true;
            // 
            // guna2GroupBox1
            // 
            guna2GroupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox1.BackColor = Color.Transparent;
            guna2GroupBox1.BorderColor = Color.White;
            guna2GroupBox1.BorderRadius = 20;
            guna2GroupBox1.BorderThickness = 0;
            guna2GroupBox1.Controls.Add(label6);
            guna2GroupBox1.Controls.Add(btnSearch);
            guna2GroupBox1.Controls.Add(btnAdd);
            guna2GroupBox1.Controls.Add(inputSearch);
            guna2GroupBox1.Controls.Add(label7);
            guna2GroupBox1.CustomBorderColor = Color.White;
            guna2GroupBox1.CustomizableEdges = customizableEdges213;
            guna2GroupBox1.Font = new Font("Segoe UI", 9F);
            guna2GroupBox1.ForeColor = Color.White;
            guna2GroupBox1.Location = new Point(6, 3);
            guna2GroupBox1.Name = "guna2GroupBox1";
            guna2GroupBox1.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox1.ShadowDecoration.CustomizableEdges = customizableEdges214;
            guna2GroupBox1.ShadowDecoration.Depth = 7;
            guna2GroupBox1.ShadowDecoration.Enabled = true;
            guna2GroupBox1.ShadowDecoration.Shadow = new Padding(5, 0, 5, 5);
            guna2GroupBox1.Size = new Size(1129, 111);
            guna2GroupBox1.TabIndex = 10;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = Color.Transparent;
            label6.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.Black;
            label6.Location = new Point(8, 9);
            label6.Name = "label6";
            label6.Size = new Size(140, 30);
            label6.TabIndex = 7;
            label6.Text = "Container List";
            // 
            // btnSearch
            // 
            btnSearch.BorderRadius = 12;
            btnSearch.CustomizableEdges = customizableEdges207;
            btnSearch.FillColor = Color.LightGray;
            btnSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSearch.ForeColor = Color.White;
            btnSearch.Image = (Image)resources.GetObject("btnSearch.Image");
            btnSearch.ImageSize = new Size(15, 15);
            btnSearch.Location = new Point(6, 68);
            btnSearch.Name = "btnSearch";
            btnSearch.ShadowDecoration.CustomizableEdges = customizableEdges208;
            btnSearch.Size = new Size(37, 37);
            btnSearch.TabIndex = 2;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.BorderRadius = 12;
            btnAdd.CustomizableEdges = customizableEdges209;
            btnAdd.FillColor = Color.FromArgb(51, 71, 255);
            btnAdd.Font = new Font("Segoe UI", 9F);
            btnAdd.ForeColor = Color.White;
            btnAdd.Image = (Image)resources.GetObject("btnAdd.Image");
            btnAdd.ImageSize = new Size(15, 15);
            btnAdd.Location = new Point(1018, 68);
            btnAdd.Name = "btnAdd";
            btnAdd.ShadowDecoration.CustomizableEdges = customizableEdges210;
            btnAdd.Size = new Size(105, 37);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add New";
            // 
            // inputSearch
            // 
            inputSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            inputSearch.BorderColor = Color.Silver;
            inputSearch.BorderRadius = 12;
            inputSearch.CustomizableEdges = customizableEdges211;
            inputSearch.DefaultText = "Enter search value";
            inputSearch.Font = new Font("Segoe UI", 9F);
            inputSearch.ForeColor = Color.Silver;
            inputSearch.Location = new Point(49, 68);
            inputSearch.Name = "inputSearch";
            inputSearch.PlaceholderForeColor = Color.LightGray;
            inputSearch.PlaceholderText = "";
            inputSearch.SelectedText = "";
            inputSearch.ShadowDecoration.CustomizableEdges = customizableEdges212;
            inputSearch.Size = new Size(963, 37);
            inputSearch.TabIndex = 1;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = Color.Transparent;
            label7.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.Silver;
            label7.Location = new Point(10, 39);
            label7.Name = "label7";
            label7.Size = new Size(335, 21);
            label7.TabIndex = 9;
            label7.Text = "Here you will see a list of all existing containers\r\n";
            // 
            // containerGrid
            // 
            dataGridViewCellStyle21.BackColor = Color.White;
            containerGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle21;
            containerGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            containerGrid.BackgroundColor = Color.LightGray;
            dataGridViewCellStyle22.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle22.BackColor = Color.White;
            dataGridViewCellStyle22.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle22.ForeColor = Color.Black;
            dataGridViewCellStyle22.SelectionBackColor = SystemColors.Control;
            dataGridViewCellStyle22.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle22.WrapMode = DataGridViewTriState.True;
            containerGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle22;
            containerGrid.ColumnHeadersHeight = 4;
            containerGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dataGridViewCellStyle23.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle23.BackColor = Color.White;
            dataGridViewCellStyle23.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle23.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle23.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle23.SelectionForeColor = Color.White;
            dataGridViewCellStyle23.WrapMode = DataGridViewTriState.True;
            containerGrid.DefaultCellStyle = dataGridViewCellStyle23;
            containerGrid.GridColor = Color.LightGray;
            containerGrid.Location = new Point(9, 127);
            containerGrid.Name = "containerGrid";
            dataGridViewCellStyle24.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle24.BackColor = SystemColors.Control;
            dataGridViewCellStyle24.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle24.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle24.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle24.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle24.WrapMode = DataGridViewTriState.True;
            containerGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle24;
            containerGrid.RowHeadersVisible = false;
            containerGrid.Size = new Size(1120, 917);
            containerGrid.TabIndex = 6;
            containerGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            containerGrid.ThemeStyle.AlternatingRowsStyle.Font = null;
            containerGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            containerGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            containerGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            containerGrid.ThemeStyle.BackColor = Color.LightGray;
            containerGrid.ThemeStyle.GridColor = Color.LightGray;
            containerGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            containerGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            containerGrid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            containerGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            containerGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            containerGrid.ThemeStyle.HeaderStyle.Height = 4;
            containerGrid.ThemeStyle.ReadOnly = false;
            containerGrid.ThemeStyle.RowsStyle.BackColor = Color.White;
            containerGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            containerGrid.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            containerGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            containerGrid.ThemeStyle.RowsStyle.Height = 25;
            containerGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            containerGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // tabEdit
            // 
            tabEdit.Controls.Add(guna2GroupBox3);
            tabEdit.Controls.Add(guna2GroupBox2);
            tabEdit.Location = new Point(4, 44);
            tabEdit.Name = "tabEdit";
            tabEdit.Padding = new Padding(3);
            tabEdit.Size = new Size(1143, 1052);
            tabEdit.TabIndex = 1;
            tabEdit.Text = "Edit";
            tabEdit.UseVisualStyleBackColor = true;
            // 
            // guna2GroupBox3
            // 
            guna2GroupBox3.Anchor = AnchorStyles.Top;
            guna2GroupBox3.BackColor = Color.Transparent;
            guna2GroupBox3.BorderColor = Color.White;
            guna2GroupBox3.BorderRadius = 15;
            guna2GroupBox3.Controls.Add(inputContainerName);
            guna2GroupBox3.Controls.Add(numberContainerId);
            guna2GroupBox3.Controls.Add(btnSave);
            guna2GroupBox3.Controls.Add(btnCancel);
            guna2GroupBox3.Controls.Add(inputContainerNote);
            guna2GroupBox3.Controls.Add(label24);
            guna2GroupBox3.Controls.Add(label3);
            guna2GroupBox3.Controls.Add(guna2Button3);
            guna2GroupBox3.Controls.Add(label25);
            guna2GroupBox3.CustomBorderColor = Color.White;
            guna2GroupBox3.CustomizableEdges = customizableEdges227;
            guna2GroupBox3.Font = new Font("Segoe UI", 9F);
            guna2GroupBox3.ForeColor = Color.Black;
            guna2GroupBox3.Location = new Point(368, 133);
            guna2GroupBox3.Name = "guna2GroupBox3";
            guna2GroupBox3.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox3.ShadowDecoration.CustomizableEdges = customizableEdges228;
            guna2GroupBox3.ShadowDecoration.Depth = 7;
            guna2GroupBox3.ShadowDecoration.Enabled = true;
            guna2GroupBox3.Size = new Size(379, 312);
            guna2GroupBox3.TabIndex = 20;
            // 
            // inputContainerName
            // 
            inputContainerName.BorderColor = Color.FromArgb(224, 224, 224);
            inputContainerName.BorderRadius = 8;
            inputContainerName.CustomizableEdges = customizableEdges215;
            inputContainerName.DefaultText = "";
            inputContainerName.Font = new Font("Segoe UI", 9F);
            inputContainerName.ForeColor = Color.Black;
            inputContainerName.Location = new Point(197, 71);
            inputContainerName.Name = "inputContainerName";
            inputContainerName.PlaceholderForeColor = Color.Silver;
            inputContainerName.PlaceholderText = "Write here...";
            inputContainerName.SelectedText = "";
            inputContainerName.ShadowDecoration.CustomizableEdges = customizableEdges216;
            inputContainerName.Size = new Size(153, 33);
            inputContainerName.TabIndex = 3;
            // 
            // numberContainerId
            // 
            numberContainerId.BackColor = Color.Transparent;
            numberContainerId.BorderColor = Color.FromArgb(224, 224, 224);
            numberContainerId.BorderRadius = 12;
            numberContainerId.CustomizableEdges = customizableEdges217;
            numberContainerId.Font = new Font("Segoe UI", 9F);
            numberContainerId.Location = new Point(5, 71);
            numberContainerId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberContainerId.Name = "numberContainerId";
            numberContainerId.ShadowDecoration.CustomizableEdges = customizableEdges218;
            numberContainerId.Size = new Size(136, 33);
            numberContainerId.TabIndex = 1;
            numberContainerId.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // btnSave
            // 
            btnSave.BorderRadius = 12;
            btnSave.CustomizableEdges = customizableEdges219;
            btnSave.FillColor = Color.FromArgb(51, 71, 255);
            btnSave.Font = new Font("Segoe UI", 9F);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(255, 274);
            btnSave.Name = "btnSave";
            btnSave.ShadowDecoration.CustomizableEdges = customizableEdges220;
            btnSave.Size = new Size(119, 33);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save Changes";
            // 
            // btnCancel
            // 
            btnCancel.BorderRadius = 12;
            btnCancel.CustomizableEdges = customizableEdges221;
            btnCancel.FillColor = Color.FromArgb(224, 224, 224);
            btnCancel.Font = new Font("Segoe UI", 9F);
            btnCancel.ForeColor = Color.Gray;
            btnCancel.Location = new Point(5, 274);
            btnCancel.Name = "btnCancel";
            btnCancel.ShadowDecoration.CustomizableEdges = customizableEdges222;
            btnCancel.Size = new Size(84, 33);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            // 
            // inputContainerNote
            // 
            inputContainerNote.BorderColor = Color.FromArgb(224, 224, 224);
            inputContainerNote.BorderRadius = 12;
            inputContainerNote.CustomizableEdges = customizableEdges223;
            inputContainerNote.DefaultText = "";
            inputContainerNote.Font = new Font("Segoe UI", 9F);
            inputContainerNote.ForeColor = Color.Black;
            inputContainerNote.Location = new Point(5, 144);
            inputContainerNote.Multiline = true;
            inputContainerNote.Name = "inputContainerNote";
            inputContainerNote.PlaceholderText = "Write here...";
            inputContainerNote.SelectedText = "";
            inputContainerNote.ShadowDecoration.CustomizableEdges = customizableEdges224;
            inputContainerNote.Size = new Size(345, 116);
            inputContainerNote.TabIndex = 5;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label24.ForeColor = Color.Black;
            label24.Location = new Point(197, 51);
            label24.Name = "label24";
            label24.Size = new Size(103, 17);
            label24.TabIndex = 7;
            label24.Text = "Container Name";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.Black;
            label3.Location = new Point(5, 126);
            label3.Name = "label3";
            label3.Size = new Size(88, 15);
            label3.TabIndex = 4;
            label3.Text = "Container Note";
            // 
            // guna2Button3
            // 
            guna2Button3.BackColor = Color.White;
            guna2Button3.BorderRadius = 9;
            guna2Button3.CustomizableEdges = customizableEdges225;
            guna2Button3.DisabledState.BorderColor = Color.DarkGray;
            guna2Button3.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button3.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button3.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button3.FillColor = Color.White;
            guna2Button3.FocusedColor = Color.White;
            guna2Button3.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button3.ForeColor = Color.Black;
            guna2Button3.Image = (Image)resources.GetObject("guna2Button3.Image");
            guna2Button3.ImageSize = new Size(15, 15);
            guna2Button3.Location = new Point(5, 5);
            guna2Button3.Name = "guna2Button3";
            guna2Button3.PressedColor = Color.White;
            guna2Button3.ShadowDecoration.CustomizableEdges = customizableEdges226;
            guna2Button3.Size = new Size(119, 43);
            guna2Button3.TabIndex = 1;
            guna2Button3.Tag = "Information";
            guna2Button3.Text = "Information";
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label25.ForeColor = Color.Black;
            label25.Location = new Point(5, 51);
            label25.Name = "label25";
            label25.Size = new Size(80, 17);
            label25.TabIndex = 0;
            label25.Text = "Container ID";
            // 
            // guna2GroupBox2
            // 
            guna2GroupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox2.BackColor = Color.Transparent;
            guna2GroupBox2.BorderColor = Color.White;
            guna2GroupBox2.BorderRadius = 20;
            guna2GroupBox2.BorderThickness = 0;
            guna2GroupBox2.Controls.Add(btnBackToContainerList);
            guna2GroupBox2.Controls.Add(label26);
            guna2GroupBox2.Controls.Add(guna2Button4);
            guna2GroupBox2.Controls.Add(label27);
            guna2GroupBox2.CustomBorderColor = Color.White;
            guna2GroupBox2.CustomizableEdges = customizableEdges233;
            guna2GroupBox2.Font = new Font("Segoe UI", 9F);
            guna2GroupBox2.ForeColor = Color.White;
            guna2GroupBox2.Location = new Point(8, 6);
            guna2GroupBox2.Name = "guna2GroupBox2";
            guna2GroupBox2.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox2.ShadowDecoration.CustomizableEdges = customizableEdges234;
            guna2GroupBox2.ShadowDecoration.Depth = 7;
            guna2GroupBox2.ShadowDecoration.Enabled = true;
            guna2GroupBox2.ShadowDecoration.Shadow = new Padding(5, 0, 5, 5);
            guna2GroupBox2.Size = new Size(1127, 99);
            guna2GroupBox2.TabIndex = 19;
            // 
            // btnBackToContainerList
            // 
            btnBackToContainerList.Animated = true;
            btnBackToContainerList.AutoRoundedCorners = true;
            btnBackToContainerList.BorderColor = Color.White;
            btnBackToContainerList.CustomizableEdges = customizableEdges229;
            btnBackToContainerList.DisabledState.BorderColor = Color.DarkGray;
            btnBackToContainerList.DisabledState.CustomBorderColor = Color.DarkGray;
            btnBackToContainerList.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnBackToContainerList.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnBackToContainerList.FillColor = Color.FromArgb(231, 231, 231);
            btnBackToContainerList.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBackToContainerList.ForeColor = Color.DarkGray;
            btnBackToContainerList.Image = (Image)resources.GetObject("btnBackToContainerList.Image");
            btnBackToContainerList.ImageSize = new Size(15, 15);
            btnBackToContainerList.Location = new Point(5, 7);
            btnBackToContainerList.Name = "btnBackToContainerList";
            btnBackToContainerList.ShadowDecoration.CustomizableEdges = customizableEdges230;
            btnBackToContainerList.Size = new Size(74, 33);
            btnBackToContainerList.TabIndex = 21;
            btnBackToContainerList.Text = "Back ";
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label26.ForeColor = Color.Black;
            label26.Location = new Point(6, 43);
            label26.Name = "label26";
            label26.Size = new Size(136, 28);
            label26.TabIndex = 15;
            label26.Text = "Container Edit";
            // 
            // guna2Button4
            // 
            guna2Button4.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2Button4.Animated = true;
            guna2Button4.BackColor = Color.Transparent;
            guna2Button4.BorderRadius = 12;
            guna2Button4.CustomizableEdges = customizableEdges231;
            guna2Button4.DisabledState.BorderColor = Color.DarkGray;
            guna2Button4.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button4.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button4.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button4.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button4.Font = new Font("Noto Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button4.ForeColor = Color.White;
            guna2Button4.ImageSize = new Size(15, 15);
            guna2Button4.Location = new Point(1972, 68);
            guna2Button4.Name = "guna2Button4";
            guna2Button4.ShadowDecoration.CustomizableEdges = customizableEdges232;
            guna2Button4.Size = new Size(105, 37);
            guna2Button4.TabIndex = 3;
            guna2Button4.Text = "Add New";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label27.ForeColor = Color.Silver;
            label27.Location = new Point(6, 71);
            label27.Name = "label27";
            label27.Size = new Size(430, 21);
            label27.TabIndex = 16;
            label27.Text = "Here you can change or provide information about container\r\n";
            // 
            // tabProfile
            // 
            tabProfile.Controls.Add(guna2GroupBox8);
            tabProfile.Controls.Add(guna2GroupBox6);
            tabProfile.Controls.Add(guna2GroupBox7);
            tabProfile.Location = new Point(4, 44);
            tabProfile.Name = "tabProfile";
            tabProfile.Padding = new Padding(3);
            tabProfile.Size = new Size(1143, 1052);
            tabProfile.TabIndex = 2;
            tabProfile.Text = "Profile";
            tabProfile.UseVisualStyleBackColor = true;
            // 
            // guna2GroupBox8
            // 
            guna2GroupBox8.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            guna2GroupBox8.BackColor = Color.Transparent;
            guna2GroupBox8.BorderColor = Color.White;
            guna2GroupBox8.BorderRadius = 15;
            guna2GroupBox8.Controls.Add(guna2GroupBox4);
            guna2GroupBox8.Controls.Add(btnCancelProfile2);
            guna2GroupBox8.Controls.Add(guna2Button5);
            guna2GroupBox8.Controls.Add(scheduleGrid);
            guna2GroupBox8.Controls.Add(guna2Button7);
            guna2GroupBox8.Controls.Add(guna2Button1);
            guna2GroupBox8.CustomBorderColor = Color.White;
            guna2GroupBox8.CustomizableEdges = customizableEdges255;
            guna2GroupBox8.Font = new Font("Segoe UI", 9F);
            guna2GroupBox8.ForeColor = Color.Black;
            guna2GroupBox8.Location = new Point(513, 126);
            guna2GroupBox8.Name = "guna2GroupBox8";
            guna2GroupBox8.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox8.ShadowDecoration.CustomizableEdges = customizableEdges256;
            guna2GroupBox8.ShadowDecoration.Depth = 7;
            guna2GroupBox8.ShadowDecoration.Enabled = true;
            guna2GroupBox8.Size = new Size(482, 918);
            guna2GroupBox8.TabIndex = 29;
            // 
            // guna2GroupBox4
            // 
            guna2GroupBox4.Anchor = AnchorStyles.Top;
            guna2GroupBox4.BackColor = Color.Transparent;
            guna2GroupBox4.BorderColor = Color.Black;
            guna2GroupBox4.BorderRadius = 20;
            guna2GroupBox4.BorderThickness = 0;
            guna2GroupBox4.Controls.Add(guna2Button8);
            guna2GroupBox4.Controls.Add(guna2Button2);
            guna2GroupBox4.Controls.Add(label5);
            guna2GroupBox4.Controls.Add(btnScheduleSearch);
            guna2GroupBox4.Controls.Add(btnScheduleAdd);
            guna2GroupBox4.Controls.Add(inputScheduleSearch);
            guna2GroupBox4.CustomBorderColor = Color.White;
            guna2GroupBox4.CustomizableEdges = customizableEdges245;
            guna2GroupBox4.Font = new Font("Segoe UI", 9F);
            guna2GroupBox4.ForeColor = Color.White;
            guna2GroupBox4.Location = new Point(7, 7);
            guna2GroupBox4.Name = "guna2GroupBox4";
            guna2GroupBox4.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox4.ShadowDecoration.CustomizableEdges = customizableEdges246;
            guna2GroupBox4.ShadowDecoration.Depth = 25;
            guna2GroupBox4.ShadowDecoration.Enabled = true;
            guna2GroupBox4.ShadowDecoration.Shadow = new Padding(0, 0, 0, 5);
            guna2GroupBox4.Size = new Size(467, 128);
            guna2GroupBox4.TabIndex = 28;
            // 
            // guna2Button8
            // 
            guna2Button8.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2Button8.Animated = true;
            guna2Button8.BackColor = Color.Transparent;
            guna2Button8.BorderRadius = 12;
            guna2Button8.CustomizableEdges = customizableEdges235;
            guna2Button8.DisabledState.BorderColor = Color.DarkGray;
            guna2Button8.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button8.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button8.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button8.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button8.Font = new Font("Noto Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button8.ForeColor = Color.White;
            guna2Button8.ImageSize = new Size(15, 15);
            guna2Button8.Location = new Point(3158, 68);
            guna2Button8.Name = "guna2Button8";
            guna2Button8.ShadowDecoration.CustomizableEdges = customizableEdges236;
            guna2Button8.Size = new Size(105, 37);
            guna2Button8.TabIndex = 3;
            guna2Button8.Text = "Add New";
            // 
            // guna2Button2
            // 
            guna2Button2.BackColor = Color.White;
            guna2Button2.BorderRadius = 9;
            guna2Button2.CustomizableEdges = customizableEdges237;
            guna2Button2.DisabledState.BorderColor = Color.DarkGray;
            guna2Button2.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button2.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button2.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button2.FillColor = Color.White;
            guna2Button2.FocusedColor = Color.White;
            guna2Button2.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button2.ForeColor = Color.Black;
            guna2Button2.Image = (Image)resources.GetObject("guna2Button2.Image");
            guna2Button2.ImageSize = new Size(15, 15);
            guna2Button2.Location = new Point(6, 6);
            guna2Button2.Name = "guna2Button2";
            guna2Button2.PressedColor = Color.White;
            guna2Button2.ShadowDecoration.CustomizableEdges = customizableEdges238;
            guna2Button2.Size = new Size(176, 42);
            guna2Button2.TabIndex = 1;
            guna2Button2.Tag = "Information";
            guna2Button2.Text = "Availability Table";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.Silver;
            label5.Location = new Point(6, 51);
            label5.Name = "label5";
            label5.Size = new Size(335, 21);
            label5.TabIndex = 9;
            label5.Text = "Here you will see a list of all existing containers\r\n";
            // 
            // btnScheduleSearch
            // 
            btnScheduleSearch.BorderRadius = 12;
            btnScheduleSearch.CustomizableEdges = customizableEdges239;
            btnScheduleSearch.FillColor = Color.LightGray;
            btnScheduleSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnScheduleSearch.ForeColor = Color.White;
            btnScheduleSearch.Image = (Image)resources.GetObject("btnScheduleSearch.Image");
            btnScheduleSearch.ImageSize = new Size(15, 15);
            btnScheduleSearch.Location = new Point(7, 84);
            btnScheduleSearch.Name = "btnScheduleSearch";
            btnScheduleSearch.ShadowDecoration.CustomizableEdges = customizableEdges240;
            btnScheduleSearch.Size = new Size(37, 37);
            btnScheduleSearch.TabIndex = 6;
            // 
            // btnScheduleAdd
            // 
            btnScheduleAdd.BorderRadius = 12;
            btnScheduleAdd.CustomizableEdges = customizableEdges241;
            btnScheduleAdd.FillColor = Color.FromArgb(51, 71, 255);
            btnScheduleAdd.Font = new Font("Segoe UI", 9F);
            btnScheduleAdd.ForeColor = Color.White;
            btnScheduleAdd.Image = (Image)resources.GetObject("btnScheduleAdd.Image");
            btnScheduleAdd.ImageSize = new Size(15, 15);
            btnScheduleAdd.Location = new Point(355, 84);
            btnScheduleAdd.Name = "btnScheduleAdd";
            btnScheduleAdd.ShadowDecoration.CustomizableEdges = customizableEdges242;
            btnScheduleAdd.Size = new Size(105, 37);
            btnScheduleAdd.TabIndex = 7;
            btnScheduleAdd.Text = "Add New";
            // 
            // inputScheduleSearch
            // 
            inputScheduleSearch.BorderColor = Color.Silver;
            inputScheduleSearch.BorderRadius = 12;
            inputScheduleSearch.CustomizableEdges = customizableEdges243;
            inputScheduleSearch.DefaultText = "";
            inputScheduleSearch.Font = new Font("Segoe UI", 9F);
            inputScheduleSearch.Location = new Point(50, 84);
            inputScheduleSearch.Name = "inputScheduleSearch";
            inputScheduleSearch.PlaceholderForeColor = Color.LightGray;
            inputScheduleSearch.PlaceholderText = "Enter search value";
            inputScheduleSearch.SelectedText = "";
            inputScheduleSearch.ShadowDecoration.CustomizableEdges = customizableEdges244;
            inputScheduleSearch.Size = new Size(299, 37);
            inputScheduleSearch.TabIndex = 5;
            // 
            // btnCancelProfile2
            // 
            btnCancelProfile2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancelProfile2.BorderRadius = 12;
            btnCancelProfile2.CustomizableEdges = customizableEdges247;
            btnCancelProfile2.DisabledState.BorderColor = Color.DarkGray;
            btnCancelProfile2.DisabledState.CustomBorderColor = Color.DarkGray;
            btnCancelProfile2.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnCancelProfile2.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnCancelProfile2.FillColor = Color.FromArgb(224, 224, 224);
            btnCancelProfile2.Font = new Font("Segoe UI", 9F);
            btnCancelProfile2.ForeColor = Color.Silver;
            btnCancelProfile2.Location = new Point(5, 1379);
            btnCancelProfile2.Name = "btnCancelProfile2";
            btnCancelProfile2.ShadowDecoration.CustomizableEdges = customizableEdges248;
            btnCancelProfile2.Size = new Size(65, 33);
            btnCancelProfile2.TabIndex = 27;
            btnCancelProfile2.Text = "Cancel";
            // 
            // guna2Button5
            // 
            guna2Button5.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button5.BorderRadius = 12;
            guna2Button5.CustomizableEdges = customizableEdges249;
            guna2Button5.DisabledState.BorderColor = Color.DarkGray;
            guna2Button5.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button5.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button5.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button5.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button5.Font = new Font("Segoe UI", 9F);
            guna2Button5.ForeColor = Color.White;
            guna2Button5.Location = new Point(493, 1878);
            guna2Button5.Name = "guna2Button5";
            guna2Button5.ShadowDecoration.CustomizableEdges = customizableEdges250;
            guna2Button5.Size = new Size(114, 33);
            guna2Button5.TabIndex = 7;
            guna2Button5.Text = "Save Changes";
            // 
            // scheduleGrid
            // 
            dataGridViewCellStyle25.BackColor = Color.White;
            scheduleGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle25;
            scheduleGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleGrid.BackgroundColor = Color.LightGray;
            dataGridViewCellStyle26.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle26.BackColor = Color.White;
            dataGridViewCellStyle26.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle26.ForeColor = Color.Black;
            dataGridViewCellStyle26.SelectionBackColor = SystemColors.Control;
            dataGridViewCellStyle26.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle26.WrapMode = DataGridViewTriState.True;
            scheduleGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle26;
            scheduleGrid.ColumnHeadersHeight = 4;
            scheduleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dataGridViewCellStyle27.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle27.BackColor = Color.White;
            dataGridViewCellStyle27.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle27.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle27.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle27.SelectionForeColor = Color.White;
            dataGridViewCellStyle27.WrapMode = DataGridViewTriState.True;
            scheduleGrid.DefaultCellStyle = dataGridViewCellStyle27;
            scheduleGrid.GridColor = Color.LightGray;
            scheduleGrid.Location = new Point(14, 151);
            scheduleGrid.Name = "scheduleGrid";
            dataGridViewCellStyle28.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle28.BackColor = SystemColors.Control;
            dataGridViewCellStyle28.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle28.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle28.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle28.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle28.WrapMode = DataGridViewTriState.True;
            scheduleGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle28;
            scheduleGrid.RowHeadersVisible = false;
            scheduleGrid.Size = new Size(453, 753);
            scheduleGrid.TabIndex = 6;
            scheduleGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            scheduleGrid.ThemeStyle.AlternatingRowsStyle.Font = null;
            scheduleGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            scheduleGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            scheduleGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            scheduleGrid.ThemeStyle.BackColor = Color.LightGray;
            scheduleGrid.ThemeStyle.GridColor = Color.LightGray;
            scheduleGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            scheduleGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            scheduleGrid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            scheduleGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            scheduleGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            scheduleGrid.ThemeStyle.HeaderStyle.Height = 4;
            scheduleGrid.ThemeStyle.ReadOnly = false;
            scheduleGrid.ThemeStyle.RowsStyle.BackColor = Color.White;
            scheduleGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            scheduleGrid.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            scheduleGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            scheduleGrid.ThemeStyle.RowsStyle.Height = 25;
            scheduleGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            scheduleGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // guna2Button7
            // 
            guna2Button7.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            guna2Button7.Animated = true;
            guna2Button7.BorderRadius = 12;
            guna2Button7.CustomizableEdges = customizableEdges251;
            guna2Button7.DisabledState.BorderColor = Color.DarkGray;
            guna2Button7.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button7.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button7.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button7.FillColor = Color.FromArgb(224, 224, 224);
            guna2Button7.Font = new Font("Segoe UI", 9F);
            guna2Button7.ForeColor = Color.Gray;
            guna2Button7.Location = new Point(5, 1878);
            guna2Button7.Name = "guna2Button7";
            guna2Button7.ShadowDecoration.CustomizableEdges = customizableEdges252;
            guna2Button7.Size = new Size(84, 33);
            guna2Button7.TabIndex = 8;
            guna2Button7.Text = "Cancel";
            // 
            // guna2Button1
            // 
            guna2Button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button1.BorderRadius = 12;
            guna2Button1.CustomizableEdges = customizableEdges253;
            guna2Button1.DisabledState.BorderColor = Color.DarkGray;
            guna2Button1.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button1.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button1.Font = new Font("Segoe UI", 9F);
            guna2Button1.ForeColor = Color.White;
            guna2Button1.Location = new Point(469, 1379);
            guna2Button1.Name = "guna2Button1";
            guna2Button1.ShadowDecoration.CustomizableEdges = customizableEdges254;
            guna2Button1.Size = new Size(73, 33);
            guna2Button1.TabIndex = 5;
            guna2Button1.Text = "Edit";
            // 
            // guna2GroupBox6
            // 
            guna2GroupBox6.Anchor = AnchorStyles.Top;
            guna2GroupBox6.BackColor = Color.Transparent;
            guna2GroupBox6.BorderColor = Color.White;
            guna2GroupBox6.BorderRadius = 17;
            guna2GroupBox6.BorderThickness = 0;
            guna2GroupBox6.Controls.Add(lblContainerNote);
            guna2GroupBox6.Controls.Add(label2);
            guna2GroupBox6.Controls.Add(btnCancelProfile);
            guna2GroupBox6.Controls.Add(btnEdit);
            guna2GroupBox6.Controls.Add(btnDelete);
            guna2GroupBox6.Controls.Add(labelId);
            guna2GroupBox6.Controls.Add(label8);
            guna2GroupBox6.Controls.Add(label1);
            guna2GroupBox6.Controls.Add(guna2Button9);
            guna2GroupBox6.Controls.Add(lblContainerName);
            guna2GroupBox6.CustomBorderColor = Color.White;
            guna2GroupBox6.CustomizableEdges = customizableEdges267;
            guna2GroupBox6.Font = new Font("Segoe UI", 9F);
            guna2GroupBox6.ForeColor = Color.Black;
            guna2GroupBox6.Location = new Point(136, 126);
            guna2GroupBox6.Name = "guna2GroupBox6";
            guna2GroupBox6.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox6.ShadowDecoration.CustomizableEdges = customizableEdges268;
            guna2GroupBox6.ShadowDecoration.Depth = 7;
            guna2GroupBox6.ShadowDecoration.Enabled = true;
            guna2GroupBox6.Size = new Size(353, 291);
            guna2GroupBox6.TabIndex = 28;
            // 
            // lblContainerNote
            // 
            lblContainerNote.BorderRadius = 12;
            lblContainerNote.CustomizableEdges = customizableEdges257;
            lblContainerNote.DefaultText = "";
            lblContainerNote.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            lblContainerNote.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            lblContainerNote.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            lblContainerNote.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            lblContainerNote.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            lblContainerNote.Font = new Font("Segoe UI", 9F);
            lblContainerNote.ForeColor = Color.Black;
            lblContainerNote.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
            lblContainerNote.Location = new Point(12, 129);
            lblContainerNote.Multiline = true;
            lblContainerNote.Name = "lblContainerNote";
            lblContainerNote.PlaceholderText = "";
            lblContainerNote.ReadOnly = true;
            lblContainerNote.SelectedText = "";
            lblContainerNote.ShadowDecoration.CustomizableEdges = customizableEdges258;
            lblContainerNote.Size = new Size(328, 102);
            lblContainerNote.TabIndex = 30;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.Gray;
            label2.Location = new Point(198, 21);
            label2.Name = "label2";
            label2.Size = new Size(76, 15);
            label2.TabIndex = 7;
            label2.Text = "Container ID:";
            // 
            // btnCancelProfile
            // 
            btnCancelProfile.BorderRadius = 12;
            btnCancelProfile.CustomizableEdges = customizableEdges259;
            btnCancelProfile.FillColor = Color.FromArgb(224, 224, 224);
            btnCancelProfile.Font = new Font("Segoe UI", 9F);
            btnCancelProfile.ForeColor = Color.Silver;
            btnCancelProfile.Location = new Point(6, 251);
            btnCancelProfile.Name = "btnCancelProfile";
            btnCancelProfile.ShadowDecoration.CustomizableEdges = customizableEdges260;
            btnCancelProfile.Size = new Size(65, 33);
            btnCancelProfile.TabIndex = 11;
            btnCancelProfile.Text = "Cancel";
            // 
            // btnEdit
            // 
            btnEdit.BorderRadius = 12;
            btnEdit.CustomizableEdges = customizableEdges261;
            btnEdit.FillColor = Color.FromArgb(51, 71, 255);
            btnEdit.Font = new Font("Segoe UI", 9F);
            btnEdit.ForeColor = Color.White;
            btnEdit.Location = new Point(275, 251);
            btnEdit.Name = "btnEdit";
            btnEdit.ShadowDecoration.CustomizableEdges = customizableEdges262;
            btnEdit.Size = new Size(71, 33);
            btnEdit.TabIndex = 4;
            btnEdit.Text = "Edit";
            // 
            // btnDelete
            // 
            btnDelete.BorderRadius = 12;
            btnDelete.CustomizableEdges = customizableEdges263;
            btnDelete.FillColor = Color.FromArgb(255, 94, 98);
            btnDelete.Font = new Font("Segoe UI", 9F);
            btnDelete.ForeColor = Color.White;
            btnDelete.Location = new Point(198, 251);
            btnDelete.Name = "btnDelete";
            btnDelete.ShadowDecoration.CustomizableEdges = customizableEdges264;
            btnDelete.Size = new Size(71, 33);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            // 
            // labelId
            // 
            labelId.AutoSize = true;
            labelId.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelId.ForeColor = Color.Black;
            labelId.Location = new Point(279, 21);
            labelId.Name = "labelId";
            labelId.Size = new Size(13, 15);
            labelId.TabIndex = 10;
            labelId.Text = "0";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.ForeColor = Color.Gray;
            label8.Location = new Point(12, 111);
            label8.Name = "label8";
            label8.Size = new Size(91, 15);
            label8.TabIndex = 5;
            label8.Text = "Container Note:\r\n";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = Color.Gray;
            label1.Location = new Point(12, 52);
            label1.Name = "label1";
            label1.Size = new Size(97, 15);
            label1.TabIndex = 4;
            label1.Text = "Container Name:";
            // 
            // guna2Button9
            // 
            guna2Button9.BackColor = Color.White;
            guna2Button9.BorderRadius = 9;
            guna2Button9.CustomizableEdges = customizableEdges265;
            guna2Button9.DisabledState.BorderColor = Color.DarkGray;
            guna2Button9.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button9.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button9.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button9.FillColor = Color.White;
            guna2Button9.FocusedColor = Color.White;
            guna2Button9.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button9.ForeColor = Color.Black;
            guna2Button9.Image = (Image)resources.GetObject("guna2Button9.Image");
            guna2Button9.ImageSize = new Size(15, 15);
            guna2Button9.Location = new Point(6, 6);
            guna2Button9.Name = "guna2Button9";
            guna2Button9.PressedColor = Color.White;
            guna2Button9.ShadowDecoration.CustomizableEdges = customizableEdges266;
            guna2Button9.Size = new Size(186, 43);
            guna2Button9.TabIndex = 3;
            guna2Button9.Tag = "Information";
            guna2Button9.Text = "Profile Information";
            // 
            // lblContainerName
            // 
            lblContainerName.AutoSize = true;
            lblContainerName.Font = new Font("Segoe UI", 15.75F);
            lblContainerName.ForeColor = Color.Black;
            lblContainerName.Location = new Point(12, 68);
            lblContainerName.Name = "lblContainerName";
            lblContainerName.Size = new Size(170, 30);
            lblContainerName.TabIndex = 0;
            lblContainerName.Text = "Contiainer Name";
            // 
            // guna2GroupBox7
            // 
            guna2GroupBox7.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox7.BackColor = Color.Transparent;
            guna2GroupBox7.BorderColor = Color.White;
            guna2GroupBox7.BorderRadius = 20;
            guna2GroupBox7.BorderThickness = 0;
            guna2GroupBox7.Controls.Add(btnBackToContainerListFromProfile);
            guna2GroupBox7.Controls.Add(label22);
            guna2GroupBox7.Controls.Add(guna2Button10);
            guna2GroupBox7.Controls.Add(label23);
            guna2GroupBox7.CustomBorderColor = Color.White;
            guna2GroupBox7.CustomizableEdges = customizableEdges273;
            guna2GroupBox7.Font = new Font("Segoe UI", 9F);
            guna2GroupBox7.ForeColor = Color.White;
            guna2GroupBox7.Location = new Point(8, 6);
            guna2GroupBox7.Name = "guna2GroupBox7";
            guna2GroupBox7.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox7.ShadowDecoration.CustomizableEdges = customizableEdges274;
            guna2GroupBox7.ShadowDecoration.Depth = 7;
            guna2GroupBox7.ShadowDecoration.Enabled = true;
            guna2GroupBox7.ShadowDecoration.Shadow = new Padding(5, 0, 5, 5);
            guna2GroupBox7.Size = new Size(1127, 99);
            guna2GroupBox7.TabIndex = 27;
            // 
            // btnBackToContainerListFromProfile
            // 
            btnBackToContainerListFromProfile.Animated = true;
            btnBackToContainerListFromProfile.AutoRoundedCorners = true;
            btnBackToContainerListFromProfile.BorderColor = Color.White;
            btnBackToContainerListFromProfile.CustomizableEdges = customizableEdges269;
            btnBackToContainerListFromProfile.DisabledState.BorderColor = Color.DarkGray;
            btnBackToContainerListFromProfile.DisabledState.CustomBorderColor = Color.DarkGray;
            btnBackToContainerListFromProfile.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnBackToContainerListFromProfile.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnBackToContainerListFromProfile.FillColor = Color.FromArgb(231, 231, 231);
            btnBackToContainerListFromProfile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBackToContainerListFromProfile.ForeColor = Color.DarkGray;
            btnBackToContainerListFromProfile.Image = (Image)resources.GetObject("btnBackToContainerListFromProfile.Image");
            btnBackToContainerListFromProfile.ImageSize = new Size(15, 15);
            btnBackToContainerListFromProfile.Location = new Point(6, 7);
            btnBackToContainerListFromProfile.Name = "btnBackToContainerListFromProfile";
            btnBackToContainerListFromProfile.ShadowDecoration.CustomizableEdges = customizableEdges270;
            btnBackToContainerListFromProfile.Size = new Size(77, 33);
            btnBackToContainerListFromProfile.TabIndex = 14;
            btnBackToContainerListFromProfile.Text = "Back";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label22.ForeColor = Color.Black;
            label22.Location = new Point(6, 43);
            label22.Name = "label22";
            label22.Size = new Size(158, 28);
            label22.TabIndex = 15;
            label22.Text = "Container Profile";
            // 
            // guna2Button10
            // 
            guna2Button10.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2Button10.Animated = true;
            guna2Button10.BackColor = Color.Transparent;
            guna2Button10.BorderRadius = 12;
            guna2Button10.CustomizableEdges = customizableEdges271;
            guna2Button10.DisabledState.BorderColor = Color.DarkGray;
            guna2Button10.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button10.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button10.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button10.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button10.Font = new Font("Noto Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button10.ForeColor = Color.White;
            guna2Button10.ImageSize = new Size(15, 15);
            guna2Button10.Location = new Point(2991, 68);
            guna2Button10.Name = "guna2Button10";
            guna2Button10.ShadowDecoration.CustomizableEdges = customizableEdges272;
            guna2Button10.Size = new Size(105, 37);
            guna2Button10.TabIndex = 3;
            guna2Button10.Text = "Add New";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label23.ForeColor = Color.Silver;
            label23.Location = new Point(6, 71);
            label23.Name = "label23";
            label23.Size = new Size(317, 21);
            label23.TabIndex = 16;
            label23.Text = "Here you can see a detailed container profile\r\n";
            // 
            // tabScheduleEdit
            // 
            tabScheduleEdit.Controls.Add(panel3);
            tabScheduleEdit.Controls.Add(panel2);
            tabScheduleEdit.Controls.Add(panel1);
            tabScheduleEdit.Location = new Point(4, 44);
            tabScheduleEdit.Name = "tabScheduleEdit";
            tabScheduleEdit.Padding = new Padding(3);
            tabScheduleEdit.Size = new Size(1143, 1052);
            tabScheduleEdit.TabIndex = 3;
            tabScheduleEdit.Text = "Schedule Edit";
            tabScheduleEdit.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.AutoScroll = true;
            panel3.Controls.Add(guna2GroupBox19);
            panel3.Controls.Add(guna2GroupBox16);
            panel3.Controls.Add(guna2GroupBox5);
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(3, 118);
            panel3.MaximumSize = new Size(417, 931);
            panel3.Name = "panel3";
            panel3.Size = new Size(417, 931);
            panel3.TabIndex = 35;
            // 
            // guna2GroupBox19
            // 
            guna2GroupBox19.BackColor = Color.Transparent;
            guna2GroupBox19.BorderColor = Color.White;
            guna2GroupBox19.BorderRadius = 15;
            guna2GroupBox19.Controls.Add(label37);
            guna2GroupBox19.Controls.Add(guna2Button30);
            guna2GroupBox19.Controls.Add(btnSearchEmployeeInAvailabilityEdit);
            guna2GroupBox19.Controls.Add(textBoxSearchValue3FromScheduleEdit);
            guna2GroupBox19.Controls.Add(btnRemoveEmployeeFromGroup);
            guna2GroupBox19.Controls.Add(lblEmployeeId);
            guna2GroupBox19.Controls.Add(comboboxEmployee);
            guna2GroupBox19.Controls.Add(label29);
            guna2GroupBox19.Controls.Add(btnAddEmployeeToGroup);
            guna2GroupBox19.Controls.Add(label36);
            guna2GroupBox19.Controls.Add(btnShowHideEmployee);
            guna2GroupBox19.Controls.Add(guna2Button28);
            guna2GroupBox19.CustomBorderColor = Color.White;
            guna2GroupBox19.CustomizableEdges = customizableEdges291;
            guna2GroupBox19.Font = new Font("Segoe UI", 9F);
            guna2GroupBox19.ForeColor = Color.White;
            guna2GroupBox19.Location = new Point(6, 703);
            guna2GroupBox19.Name = "guna2GroupBox19";
            guna2GroupBox19.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox19.ShadowDecoration.CustomizableEdges = customizableEdges292;
            guna2GroupBox19.ShadowDecoration.Depth = 7;
            guna2GroupBox19.ShadowDecoration.Enabled = true;
            guna2GroupBox19.Size = new Size(379, 216);
            guna2GroupBox19.TabIndex = 36;
            // 
            // label37
            // 
            label37.AutoSize = true;
            label37.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label37.ForeColor = Color.Black;
            label37.Location = new Point(9, 55);
            label37.Name = "label37";
            label37.Size = new Size(52, 17);
            label37.TabIndex = 42;
            label37.Text = "Search:";
            // 
            // guna2Button30
            // 
            guna2Button30.BackColor = Color.White;
            guna2Button30.BorderRadius = 12;
            guna2Button30.CustomizableEdges = customizableEdges275;
            guna2Button30.FillColor = Color.FromArgb(224, 224, 224);
            guna2Button30.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button30.ForeColor = Color.Gray;
            guna2Button30.Location = new Point(6, 177);
            guna2Button30.Name = "guna2Button30";
            guna2Button30.ShadowDecoration.CustomizableEdges = customizableEdges276;
            guna2Button30.Size = new Size(84, 33);
            guna2Button30.TabIndex = 41;
            guna2Button30.Text = "Cancel";
            // 
            // btnSearchEmployeeInAvailabilityEdit
            // 
            btnSearchEmployeeInAvailabilityEdit.Animated = true;
            btnSearchEmployeeInAvailabilityEdit.BackColor = Color.Transparent;
            btnSearchEmployeeInAvailabilityEdit.BorderRadius = 12;
            btnSearchEmployeeInAvailabilityEdit.CustomizableEdges = customizableEdges277;
            btnSearchEmployeeInAvailabilityEdit.DisabledState.BorderColor = Color.DarkGray;
            btnSearchEmployeeInAvailabilityEdit.DisabledState.CustomBorderColor = Color.DarkGray;
            btnSearchEmployeeInAvailabilityEdit.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnSearchEmployeeInAvailabilityEdit.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnSearchEmployeeInAvailabilityEdit.FillColor = Color.LightGray;
            btnSearchEmployeeInAvailabilityEdit.Font = new Font("Segoe UI", 9F);
            btnSearchEmployeeInAvailabilityEdit.ForeColor = Color.White;
            btnSearchEmployeeInAvailabilityEdit.Image = (Image)resources.GetObject("btnSearchEmployeeInAvailabilityEdit.Image");
            btnSearchEmployeeInAvailabilityEdit.ImageSize = new Size(15, 15);
            btnSearchEmployeeInAvailabilityEdit.Location = new Point(336, 44);
            btnSearchEmployeeInAvailabilityEdit.Name = "btnSearchEmployeeInAvailabilityEdit";
            btnSearchEmployeeInAvailabilityEdit.ShadowDecoration.CustomizableEdges = customizableEdges278;
            btnSearchEmployeeInAvailabilityEdit.Size = new Size(37, 37);
            btnSearchEmployeeInAvailabilityEdit.TabIndex = 37;
            // 
            // textBoxSearchValue3FromScheduleEdit
            // 
            textBoxSearchValue3FromScheduleEdit.Animated = true;
            textBoxSearchValue3FromScheduleEdit.BorderColor = Color.FromArgb(224, 224, 224);
            textBoxSearchValue3FromScheduleEdit.BorderRadius = 10;
            textBoxSearchValue3FromScheduleEdit.CustomizableEdges = customizableEdges279;
            textBoxSearchValue3FromScheduleEdit.DefaultText = "";
            textBoxSearchValue3FromScheduleEdit.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            textBoxSearchValue3FromScheduleEdit.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            textBoxSearchValue3FromScheduleEdit.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValue3FromScheduleEdit.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValue3FromScheduleEdit.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValue3FromScheduleEdit.Font = new Font("Segoe UI", 9F);
            textBoxSearchValue3FromScheduleEdit.ForeColor = Color.Black;
            textBoxSearchValue3FromScheduleEdit.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValue3FromScheduleEdit.Location = new Point(67, 46);
            textBoxSearchValue3FromScheduleEdit.Name = "textBoxSearchValue3FromScheduleEdit";
            textBoxSearchValue3FromScheduleEdit.PlaceholderText = "Write search value...";
            textBoxSearchValue3FromScheduleEdit.SelectedText = "";
            textBoxSearchValue3FromScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges280;
            textBoxSearchValue3FromScheduleEdit.Size = new Size(263, 37);
            textBoxSearchValue3FromScheduleEdit.TabIndex = 36;
            // 
            // btnRemoveEmployeeFromGroup
            // 
            btnRemoveEmployeeFromGroup.Animated = true;
            btnRemoveEmployeeFromGroup.BorderRadius = 12;
            btnRemoveEmployeeFromGroup.CustomizableEdges = customizableEdges281;
            btnRemoveEmployeeFromGroup.DisabledState.BorderColor = Color.DarkGray;
            btnRemoveEmployeeFromGroup.DisabledState.CustomBorderColor = Color.DarkGray;
            btnRemoveEmployeeFromGroup.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnRemoveEmployeeFromGroup.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnRemoveEmployeeFromGroup.FillColor = Color.Firebrick;
            btnRemoveEmployeeFromGroup.Font = new Font("Segoe UI", 9F);
            btnRemoveEmployeeFromGroup.ForeColor = Color.White;
            btnRemoveEmployeeFromGroup.Location = new Point(136, 177);
            btnRemoveEmployeeFromGroup.Name = "btnRemoveEmployeeFromGroup";
            btnRemoveEmployeeFromGroup.ShadowDecoration.CustomizableEdges = customizableEdges282;
            btnRemoveEmployeeFromGroup.Size = new Size(120, 33);
            btnRemoveEmployeeFromGroup.TabIndex = 34;
            btnRemoveEmployeeFromGroup.Text = "Rmove Employee";
            // 
            // lblEmployeeId
            // 
            lblEmployeeId.AutoSize = true;
            lblEmployeeId.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEmployeeId.ForeColor = Color.Black;
            lblEmployeeId.Location = new Point(348, 103);
            lblEmployeeId.Name = "lblEmployeeId";
            lblEmployeeId.Size = new Size(13, 13);
            lblEmployeeId.TabIndex = 35;
            lblEmployeeId.Text = "0";
            // 
            // comboboxEmployee
            // 
            comboboxEmployee.BackColor = Color.Transparent;
            comboboxEmployee.BorderRadius = 10;
            comboboxEmployee.CustomizableEdges = customizableEdges283;
            comboboxEmployee.DrawMode = DrawMode.OwnerDrawFixed;
            comboboxEmployee.DropDownStyle = ComboBoxStyle.DropDownList;
            comboboxEmployee.FocusedColor = Color.FromArgb(94, 148, 255);
            comboboxEmployee.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            comboboxEmployee.Font = new Font("Segoe UI", 10F);
            comboboxEmployee.ForeColor = Color.FromArgb(68, 88, 112);
            comboboxEmployee.ItemHeight = 30;
            comboboxEmployee.Location = new Point(7, 119);
            comboboxEmployee.Name = "comboboxEmployee";
            comboboxEmployee.ShadowDecoration.CustomizableEdges = customizableEdges284;
            comboboxEmployee.Size = new Size(366, 36);
            comboboxEmployee.TabIndex = 32;
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label29.ForeColor = Color.Black;
            label29.Location = new Point(16, 99);
            label29.Name = "label29";
            label29.Size = new Size(118, 17);
            label29.TabIndex = 31;
            label29.Text = "Selected Employee";
            // 
            // btnAddEmployeeToGroup
            // 
            btnAddEmployeeToGroup.Animated = true;
            btnAddEmployeeToGroup.BorderRadius = 12;
            btnAddEmployeeToGroup.CustomizableEdges = customizableEdges285;
            btnAddEmployeeToGroup.DisabledState.BorderColor = Color.DarkGray;
            btnAddEmployeeToGroup.DisabledState.CustomBorderColor = Color.DarkGray;
            btnAddEmployeeToGroup.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnAddEmployeeToGroup.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnAddEmployeeToGroup.FillColor = Color.FromArgb(51, 71, 255);
            btnAddEmployeeToGroup.Font = new Font("Segoe UI", 9F);
            btnAddEmployeeToGroup.ForeColor = Color.White;
            btnAddEmployeeToGroup.Location = new Point(262, 177);
            btnAddEmployeeToGroup.Name = "btnAddEmployeeToGroup";
            btnAddEmployeeToGroup.ShadowDecoration.CustomizableEdges = customizableEdges286;
            btnAddEmployeeToGroup.Size = new Size(111, 33);
            btnAddEmployeeToGroup.TabIndex = 33;
            btnAddEmployeeToGroup.Text = "Add Employee";
            // 
            // label36
            // 
            label36.AutoSize = true;
            label36.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label36.ForeColor = Color.Gray;
            label36.Location = new Point(275, 103);
            label36.Name = "label36";
            label36.Size = new Size(73, 13);
            label36.TabIndex = 30;
            label36.Text = "Employee ID:";
            // 
            // btnShowHideEmployee
            // 
            btnShowHideEmployee.Animated = true;
            btnShowHideEmployee.AutoRoundedCorners = true;
            btnShowHideEmployee.BorderColor = Color.White;
            btnShowHideEmployee.CustomizableEdges = customizableEdges287;
            btnShowHideEmployee.DisabledState.BorderColor = Color.DarkGray;
            btnShowHideEmployee.DisabledState.CustomBorderColor = Color.DarkGray;
            btnShowHideEmployee.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnShowHideEmployee.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnShowHideEmployee.FillColor = Color.FromArgb(231, 231, 231);
            btnShowHideEmployee.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnShowHideEmployee.ForeColor = Color.DarkGray;
            btnShowHideEmployee.Image = (Image)resources.GetObject("btnShowHideEmployee.Image");
            btnShowHideEmployee.ImageSize = new Size(15, 15);
            btnShowHideEmployee.Location = new Point(307, 5);
            btnShowHideEmployee.Name = "btnShowHideEmployee";
            btnShowHideEmployee.ShadowDecoration.CustomizableEdges = customizableEdges288;
            btnShowHideEmployee.Size = new Size(68, 33);
            btnShowHideEmployee.TabIndex = 17;
            btnShowHideEmployee.Text = "Hide";
            // 
            // guna2Button28
            // 
            guna2Button28.BackColor = Color.White;
            guna2Button28.BorderRadius = 15;
            guna2Button28.CustomizableEdges = customizableEdges289;
            guna2Button28.DisabledState.BorderColor = Color.DarkGray;
            guna2Button28.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button28.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button28.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button28.FillColor = Color.White;
            guna2Button28.FocusedColor = Color.White;
            guna2Button28.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button28.ForeColor = Color.Black;
            guna2Button28.Image = (Image)resources.GetObject("guna2Button28.Image");
            guna2Button28.ImageSize = new Size(15, 15);
            guna2Button28.Location = new Point(5, 5);
            guna2Button28.Name = "guna2Button28";
            guna2Button28.PressedColor = Color.White;
            guna2Button28.ShadowDecoration.CustomizableEdges = customizableEdges290;
            guna2Button28.Size = new Size(106, 35);
            guna2Button28.TabIndex = 1;
            guna2Button28.Tag = "Information";
            guna2Button28.Text = "Employee";
            // 
            // guna2GroupBox16
            // 
            guna2GroupBox16.BackColor = Color.Transparent;
            guna2GroupBox16.BorderColor = Color.White;
            guna2GroupBox16.BorderRadius = 15;
            guna2GroupBox16.Controls.Add(btnShowHideNote);
            guna2GroupBox16.Controls.Add(inputScheduleNote);
            guna2GroupBox16.Controls.Add(guna2Button29);
            guna2GroupBox16.CustomBorderColor = Color.White;
            guna2GroupBox16.CustomizableEdges = customizableEdges299;
            guna2GroupBox16.Font = new Font("Segoe UI", 9F);
            guna2GroupBox16.ForeColor = Color.White;
            guna2GroupBox16.Location = new Point(6, 936);
            guna2GroupBox16.Name = "guna2GroupBox16";
            guna2GroupBox16.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox16.ShadowDecoration.CustomizableEdges = customizableEdges300;
            guna2GroupBox16.ShadowDecoration.Depth = 7;
            guna2GroupBox16.ShadowDecoration.Enabled = true;
            guna2GroupBox16.Size = new Size(379, 334);
            guna2GroupBox16.TabIndex = 35;
            // 
            // btnShowHideNote
            // 
            btnShowHideNote.Animated = true;
            btnShowHideNote.AutoRoundedCorners = true;
            btnShowHideNote.BorderColor = Color.White;
            btnShowHideNote.CustomizableEdges = customizableEdges293;
            btnShowHideNote.DisabledState.BorderColor = Color.DarkGray;
            btnShowHideNote.DisabledState.CustomBorderColor = Color.DarkGray;
            btnShowHideNote.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnShowHideNote.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnShowHideNote.FillColor = Color.FromArgb(231, 231, 231);
            btnShowHideNote.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnShowHideNote.ForeColor = Color.DarkGray;
            btnShowHideNote.Image = (Image)resources.GetObject("btnShowHideNote.Image");
            btnShowHideNote.ImageSize = new Size(15, 15);
            btnShowHideNote.Location = new Point(307, 5);
            btnShowHideNote.Name = "btnShowHideNote";
            btnShowHideNote.ShadowDecoration.CustomizableEdges = customizableEdges294;
            btnShowHideNote.Size = new Size(68, 33);
            btnShowHideNote.TabIndex = 17;
            btnShowHideNote.Text = "Hide";
            // 
            // inputScheduleNote
            // 
            inputScheduleNote.BorderRadius = 10;
            inputScheduleNote.CustomizableEdges = customizableEdges295;
            inputScheduleNote.DefaultText = "";
            inputScheduleNote.Font = new Font("Segoe UI", 9F);
            inputScheduleNote.ForeColor = Color.Black;
            inputScheduleNote.Location = new Point(12, 43);
            inputScheduleNote.Multiline = true;
            inputScheduleNote.Name = "inputScheduleNote";
            inputScheduleNote.PlaceholderText = "Write here...";
            inputScheduleNote.ScrollBars = ScrollBars.Vertical;
            inputScheduleNote.SelectedText = "";
            inputScheduleNote.ShadowDecoration.CustomizableEdges = customizableEdges296;
            inputScheduleNote.Size = new Size(351, 277);
            inputScheduleNote.TabIndex = 12;
            // 
            // guna2Button29
            // 
            guna2Button29.BackColor = Color.White;
            guna2Button29.BorderRadius = 15;
            guna2Button29.CustomizableEdges = customizableEdges297;
            guna2Button29.DisabledState.BorderColor = Color.DarkGray;
            guna2Button29.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button29.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button29.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button29.FillColor = Color.White;
            guna2Button29.FocusedColor = Color.White;
            guna2Button29.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button29.ForeColor = Color.Black;
            guna2Button29.Image = (Image)resources.GetObject("guna2Button29.Image");
            guna2Button29.ImageSize = new Size(15, 15);
            guna2Button29.Location = new Point(5, 5);
            guna2Button29.Name = "guna2Button29";
            guna2Button29.PressedColor = Color.White;
            guna2Button29.ShadowDecoration.CustomizableEdges = customizableEdges298;
            guna2Button29.Size = new Size(76, 35);
            guna2Button29.TabIndex = 1;
            guna2Button29.Tag = "Information";
            guna2Button29.Text = "Note";
            // 
            // guna2GroupBox5
            // 
            guna2GroupBox5.BackColor = Color.Transparent;
            guna2GroupBox5.BorderColor = Color.White;
            guna2GroupBox5.BorderRadius = 15;
            guna2GroupBox5.Controls.Add(guna2GroupBox18);
            guna2GroupBox5.Controls.Add(guna2GroupBox17);
            guna2GroupBox5.Controls.Add(btnShowHideInfo);
            guna2GroupBox5.Controls.Add(btnScheduleCancel);
            guna2GroupBox5.Controls.Add(label14);
            guna2GroupBox5.Controls.Add(inputScheduleName);
            guna2GroupBox5.Controls.Add(label31);
            guna2GroupBox5.Controls.Add(btnGenerate);
            guna2GroupBox5.Controls.Add(label13);
            guna2GroupBox5.Controls.Add(inputMaxFull);
            guna2GroupBox5.Controls.Add(label12);
            guna2GroupBox5.Controls.Add(label11);
            guna2GroupBox5.Controls.Add(label10);
            guna2GroupBox5.Controls.Add(label9);
            guna2GroupBox5.Controls.Add(label4);
            guna2GroupBox5.Controls.Add(inputShift2);
            guna2GroupBox5.Controls.Add(label28);
            guna2GroupBox5.Controls.Add(label30);
            guna2GroupBox5.Controls.Add(inputShift1);
            guna2GroupBox5.Controls.Add(guna2Button6);
            guna2GroupBox5.Controls.Add(inputMaxConsecutiveFull);
            guna2GroupBox5.Controls.Add(label32);
            guna2GroupBox5.Controls.Add(numberScheduleId);
            guna2GroupBox5.Controls.Add(inputMaxConsecutiveDays);
            guna2GroupBox5.Controls.Add(inputPeoplePerShift);
            guna2GroupBox5.Controls.Add(inputMonth);
            guna2GroupBox5.Controls.Add(inputYear);
            guna2GroupBox5.Controls.Add(inputMaxHours);
            guna2GroupBox5.CustomBorderColor = Color.White;
            guna2GroupBox5.CustomizableEdges = customizableEdges351;
            guna2GroupBox5.Font = new Font("Segoe UI", 9F);
            guna2GroupBox5.ForeColor = Color.White;
            guna2GroupBox5.Location = new Point(6, 6);
            guna2GroupBox5.Name = "guna2GroupBox5";
            guna2GroupBox5.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox5.ShadowDecoration.CustomizableEdges = customizableEdges352;
            guna2GroupBox5.ShadowDecoration.Depth = 7;
            guna2GroupBox5.ShadowDecoration.Enabled = true;
            guna2GroupBox5.Size = new Size(379, 678);
            guna2GroupBox5.TabIndex = 31;
            // 
            // guna2GroupBox18
            // 
            guna2GroupBox18.BackColor = Color.Transparent;
            guna2GroupBox18.BorderColor = Color.White;
            guna2GroupBox18.BorderRadius = 20;
            guna2GroupBox18.Controls.Add(comboScheduleAvailability);
            guna2GroupBox18.Controls.Add(lblAvailabilityID);
            guna2GroupBox18.Controls.Add(btnSearchAvailabilityFromScheduleEdit);
            guna2GroupBox18.Controls.Add(label39);
            guna2GroupBox18.Controls.Add(guna2Button26);
            guna2GroupBox18.Controls.Add(textBoxSearchValue2FromScheduleEdit);
            guna2GroupBox18.Controls.Add(label15);
            guna2GroupBox18.CustomBorderColor = Color.White;
            guna2GroupBox18.CustomizableEdges = customizableEdges309;
            guna2GroupBox18.Font = new Font("Segoe UI", 9F);
            guna2GroupBox18.ForeColor = Color.White;
            guna2GroupBox18.Location = new Point(0, 500);
            guna2GroupBox18.Name = "guna2GroupBox18";
            guna2GroupBox18.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox18.ShadowDecoration.CustomizableEdges = customizableEdges310;
            guna2GroupBox18.ShadowDecoration.Depth = 7;
            guna2GroupBox18.ShadowDecoration.Enabled = true;
            guna2GroupBox18.Size = new Size(379, 120);
            guna2GroupBox18.TabIndex = 40;
            // 
            // comboScheduleAvailability
            // 
            comboScheduleAvailability.BackColor = Color.Transparent;
            comboScheduleAvailability.BorderRadius = 10;
            comboScheduleAvailability.CustomizableEdges = customizableEdges301;
            comboScheduleAvailability.DrawMode = DrawMode.OwnerDrawFixed;
            comboScheduleAvailability.DropDownStyle = ComboBoxStyle.DropDownList;
            comboScheduleAvailability.FocusedColor = Color.FromArgb(94, 148, 255);
            comboScheduleAvailability.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            comboScheduleAvailability.Font = new Font("Segoe UI", 10F);
            comboScheduleAvailability.ForeColor = Color.FromArgb(68, 88, 112);
            comboScheduleAvailability.ItemHeight = 30;
            comboScheduleAvailability.Location = new Point(9, 74);
            comboScheduleAvailability.Name = "comboScheduleAvailability";
            comboScheduleAvailability.ShadowDecoration.CustomizableEdges = customizableEdges302;
            comboScheduleAvailability.Size = new Size(351, 36);
            comboScheduleAvailability.TabIndex = 25;
            // 
            // lblAvailabilityID
            // 
            lblAvailabilityID.AutoSize = true;
            lblAvailabilityID.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblAvailabilityID.ForeColor = Color.Black;
            lblAvailabilityID.Location = new Point(348, 58);
            lblAvailabilityID.Name = "lblAvailabilityID";
            lblAvailabilityID.Size = new Size(13, 13);
            lblAvailabilityID.TabIndex = 44;
            lblAvailabilityID.Text = "0";
            // 
            // btnSearchAvailabilityFromScheduleEdit
            // 
            btnSearchAvailabilityFromScheduleEdit.Animated = true;
            btnSearchAvailabilityFromScheduleEdit.BackColor = Color.Transparent;
            btnSearchAvailabilityFromScheduleEdit.BorderRadius = 12;
            btnSearchAvailabilityFromScheduleEdit.CustomizableEdges = customizableEdges303;
            btnSearchAvailabilityFromScheduleEdit.DisabledState.BorderColor = Color.DarkGray;
            btnSearchAvailabilityFromScheduleEdit.DisabledState.CustomBorderColor = Color.DarkGray;
            btnSearchAvailabilityFromScheduleEdit.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnSearchAvailabilityFromScheduleEdit.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnSearchAvailabilityFromScheduleEdit.FillColor = Color.LightGray;
            btnSearchAvailabilityFromScheduleEdit.Font = new Font("Segoe UI", 9F);
            btnSearchAvailabilityFromScheduleEdit.ForeColor = Color.White;
            btnSearchAvailabilityFromScheduleEdit.Image = (Image)resources.GetObject("btnSearchAvailabilityFromScheduleEdit.Image");
            btnSearchAvailabilityFromScheduleEdit.ImageSize = new Size(15, 15);
            btnSearchAvailabilityFromScheduleEdit.Location = new Point(336, 7);
            btnSearchAvailabilityFromScheduleEdit.Name = "btnSearchAvailabilityFromScheduleEdit";
            btnSearchAvailabilityFromScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges304;
            btnSearchAvailabilityFromScheduleEdit.Size = new Size(37, 37);
            btnSearchAvailabilityFromScheduleEdit.TabIndex = 40;
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label39.ForeColor = Color.Gray;
            label39.Location = new Point(275, 58);
            label39.Name = "label39";
            label39.Size = new Size(79, 13);
            label39.TabIndex = 43;
            label39.Text = "Availability ID:";
            // 
            // guna2Button26
            // 
            guna2Button26.BackColor = Color.White;
            guna2Button26.BorderRadius = 15;
            guna2Button26.CustomizableEdges = customizableEdges305;
            guna2Button26.DisabledState.BorderColor = Color.DarkGray;
            guna2Button26.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button26.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button26.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button26.FillColor = Color.White;
            guna2Button26.FocusedColor = Color.White;
            guna2Button26.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button26.ForeColor = Color.Black;
            guna2Button26.Image = (Image)resources.GetObject("guna2Button26.Image");
            guna2Button26.ImageSize = new Size(15, 15);
            guna2Button26.Location = new Point(7, 7);
            guna2Button26.Name = "guna2Button26";
            guna2Button26.PressedColor = Color.White;
            guna2Button26.ShadowDecoration.CustomizableEdges = customizableEdges306;
            guna2Button26.Size = new Size(132, 35);
            guna2Button26.TabIndex = 39;
            guna2Button26.Tag = "Information";
            guna2Button26.Text = "Availability";
            // 
            // textBoxSearchValue2FromScheduleEdit
            // 
            textBoxSearchValue2FromScheduleEdit.Animated = true;
            textBoxSearchValue2FromScheduleEdit.BorderColor = Color.FromArgb(224, 224, 224);
            textBoxSearchValue2FromScheduleEdit.BorderRadius = 10;
            textBoxSearchValue2FromScheduleEdit.CustomizableEdges = customizableEdges307;
            textBoxSearchValue2FromScheduleEdit.DefaultText = "";
            textBoxSearchValue2FromScheduleEdit.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            textBoxSearchValue2FromScheduleEdit.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            textBoxSearchValue2FromScheduleEdit.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValue2FromScheduleEdit.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValue2FromScheduleEdit.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValue2FromScheduleEdit.Font = new Font("Segoe UI", 9F);
            textBoxSearchValue2FromScheduleEdit.ForeColor = Color.Black;
            textBoxSearchValue2FromScheduleEdit.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValue2FromScheduleEdit.Location = new Point(141, 7);
            textBoxSearchValue2FromScheduleEdit.Name = "textBoxSearchValue2FromScheduleEdit";
            textBoxSearchValue2FromScheduleEdit.PlaceholderText = "Write search value...";
            textBoxSearchValue2FromScheduleEdit.SelectedText = "";
            textBoxSearchValue2FromScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges308;
            textBoxSearchValue2FromScheduleEdit.Size = new Size(189, 37);
            textBoxSearchValue2FromScheduleEdit.TabIndex = 37;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label15.ForeColor = Color.Black;
            label15.Location = new Point(7, 54);
            label15.Name = "label15";
            label15.Size = new Size(132, 17);
            label15.TabIndex = 33;
            label15.Text = "Selected Availabilities";
            // 
            // guna2GroupBox17
            // 
            guna2GroupBox17.BackColor = Color.Transparent;
            guna2GroupBox17.BorderColor = Color.White;
            guna2GroupBox17.BorderRadius = 20;
            guna2GroupBox17.Controls.Add(guna2Button23);
            guna2GroupBox17.Controls.Add(comboScheduleShop);
            guna2GroupBox17.Controls.Add(textBoxSearchValueFromScheduleEdit);
            guna2GroupBox17.Controls.Add(btnSearchShopFromScheduleEdit);
            guna2GroupBox17.Controls.Add(labelScheduleShop);
            guna2GroupBox17.Controls.Add(label21);
            guna2GroupBox17.Controls.Add(lbShopId);
            guna2GroupBox17.CustomBorderColor = Color.White;
            guna2GroupBox17.CustomizableEdges = customizableEdges319;
            guna2GroupBox17.Font = new Font("Segoe UI", 9F);
            guna2GroupBox17.ForeColor = Color.White;
            guna2GroupBox17.Location = new Point(0, 363);
            guna2GroupBox17.Name = "guna2GroupBox17";
            guna2GroupBox17.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox17.ShadowDecoration.CustomizableEdges = customizableEdges320;
            guna2GroupBox17.ShadowDecoration.Depth = 7;
            guna2GroupBox17.ShadowDecoration.Enabled = true;
            guna2GroupBox17.Size = new Size(379, 120);
            guna2GroupBox17.TabIndex = 39;
            // 
            // guna2Button23
            // 
            guna2Button23.BackColor = Color.White;
            guna2Button23.BorderRadius = 15;
            guna2Button23.CustomizableEdges = customizableEdges311;
            guna2Button23.DisabledState.BorderColor = Color.DarkGray;
            guna2Button23.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button23.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button23.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button23.FillColor = Color.White;
            guna2Button23.FocusedColor = Color.White;
            guna2Button23.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button23.ForeColor = Color.Black;
            guna2Button23.Image = (Image)resources.GetObject("guna2Button23.Image");
            guna2Button23.ImageSize = new Size(15, 15);
            guna2Button23.Location = new Point(7, 7);
            guna2Button23.Name = "guna2Button23";
            guna2Button23.PressedColor = Color.White;
            guna2Button23.ShadowDecoration.CustomizableEdges = customizableEdges312;
            guna2Button23.Size = new Size(76, 35);
            guna2Button23.TabIndex = 39;
            guna2Button23.Tag = "Information";
            guna2Button23.Text = "Shop";
            // 
            // comboScheduleShop
            // 
            comboScheduleShop.BackColor = Color.Transparent;
            comboScheduleShop.BorderRadius = 10;
            comboScheduleShop.CustomizableEdges = customizableEdges313;
            comboScheduleShop.DrawMode = DrawMode.OwnerDrawFixed;
            comboScheduleShop.DropDownStyle = ComboBoxStyle.DropDownList;
            comboScheduleShop.FocusedColor = Color.FromArgb(94, 148, 255);
            comboScheduleShop.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            comboScheduleShop.Font = new Font("Segoe UI", 9F);
            comboScheduleShop.ForeColor = Color.FromArgb(68, 88, 112);
            comboScheduleShop.ItemHeight = 30;
            comboScheduleShop.Location = new Point(7, 75);
            comboScheduleShop.Name = "comboScheduleShop";
            comboScheduleShop.ShadowDecoration.CustomizableEdges = customizableEdges314;
            comboScheduleShop.Size = new Size(347, 36);
            comboScheduleShop.TabIndex = 4;
            // 
            // textBoxSearchValueFromScheduleEdit
            // 
            textBoxSearchValueFromScheduleEdit.Animated = true;
            textBoxSearchValueFromScheduleEdit.BorderColor = Color.FromArgb(224, 224, 224);
            textBoxSearchValueFromScheduleEdit.BorderRadius = 10;
            textBoxSearchValueFromScheduleEdit.CustomizableEdges = customizableEdges315;
            textBoxSearchValueFromScheduleEdit.DefaultText = "";
            textBoxSearchValueFromScheduleEdit.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            textBoxSearchValueFromScheduleEdit.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            textBoxSearchValueFromScheduleEdit.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValueFromScheduleEdit.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            textBoxSearchValueFromScheduleEdit.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValueFromScheduleEdit.Font = new Font("Segoe UI", 9F);
            textBoxSearchValueFromScheduleEdit.ForeColor = Color.Black;
            textBoxSearchValueFromScheduleEdit.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
            textBoxSearchValueFromScheduleEdit.Location = new Point(89, 7);
            textBoxSearchValueFromScheduleEdit.Name = "textBoxSearchValueFromScheduleEdit";
            textBoxSearchValueFromScheduleEdit.PlaceholderText = "Write search value...";
            textBoxSearchValueFromScheduleEdit.SelectedText = "";
            textBoxSearchValueFromScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges316;
            textBoxSearchValueFromScheduleEdit.Size = new Size(241, 37);
            textBoxSearchValueFromScheduleEdit.TabIndex = 37;
            // 
            // btnSearchShopFromScheduleEdit
            // 
            btnSearchShopFromScheduleEdit.Animated = true;
            btnSearchShopFromScheduleEdit.BackColor = Color.Transparent;
            btnSearchShopFromScheduleEdit.BorderRadius = 12;
            btnSearchShopFromScheduleEdit.CustomizableEdges = customizableEdges317;
            btnSearchShopFromScheduleEdit.DisabledState.BorderColor = Color.DarkGray;
            btnSearchShopFromScheduleEdit.DisabledState.CustomBorderColor = Color.DarkGray;
            btnSearchShopFromScheduleEdit.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnSearchShopFromScheduleEdit.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnSearchShopFromScheduleEdit.FillColor = Color.LightGray;
            btnSearchShopFromScheduleEdit.Font = new Font("Segoe UI", 9F);
            btnSearchShopFromScheduleEdit.ForeColor = Color.White;
            btnSearchShopFromScheduleEdit.Image = (Image)resources.GetObject("btnSearchShopFromScheduleEdit.Image");
            btnSearchShopFromScheduleEdit.ImageSize = new Size(15, 15);
            btnSearchShopFromScheduleEdit.Location = new Point(336, 7);
            btnSearchShopFromScheduleEdit.Name = "btnSearchShopFromScheduleEdit";
            btnSearchShopFromScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges318;
            btnSearchShopFromScheduleEdit.Size = new Size(37, 37);
            btnSearchShopFromScheduleEdit.TabIndex = 38;
            // 
            // labelScheduleShop
            // 
            labelScheduleShop.AutoSize = true;
            labelScheduleShop.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelScheduleShop.ForeColor = Color.Black;
            labelScheduleShop.Location = new Point(9, 55);
            labelScheduleShop.Name = "labelScheduleShop";
            labelScheduleShop.Size = new Size(91, 17);
            labelScheduleShop.TabIndex = 34;
            labelScheduleShop.Text = "Selected Shop";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label21.ForeColor = Color.Gray;
            label21.Location = new Point(281, 58);
            label21.Name = "label21";
            label21.Size = new Size(51, 13);
            label21.TabIndex = 35;
            label21.Text = "Shop ID:";
            // 
            // lbShopId
            // 
            lbShopId.AutoSize = true;
            lbShopId.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lbShopId.ForeColor = Color.Black;
            lbShopId.Location = new Point(333, 59);
            lbShopId.Name = "lbShopId";
            lbShopId.Size = new Size(13, 13);
            lbShopId.TabIndex = 36;
            lbShopId.Text = "0";
            // 
            // btnShowHideInfo
            // 
            btnShowHideInfo.Animated = true;
            btnShowHideInfo.AutoRoundedCorners = true;
            btnShowHideInfo.BorderColor = Color.White;
            btnShowHideInfo.CustomizableEdges = customizableEdges321;
            btnShowHideInfo.DisabledState.BorderColor = Color.DarkGray;
            btnShowHideInfo.DisabledState.CustomBorderColor = Color.DarkGray;
            btnShowHideInfo.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnShowHideInfo.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnShowHideInfo.FillColor = Color.FromArgb(231, 231, 231);
            btnShowHideInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnShowHideInfo.ForeColor = Color.DarkGray;
            btnShowHideInfo.Image = (Image)resources.GetObject("btnShowHideInfo.Image");
            btnShowHideInfo.ImageSize = new Size(15, 15);
            btnShowHideInfo.Location = new Point(307, 5);
            btnShowHideInfo.Name = "btnShowHideInfo";
            btnShowHideInfo.ShadowDecoration.CustomizableEdges = customizableEdges322;
            btnShowHideInfo.Size = new Size(68, 33);
            btnShowHideInfo.TabIndex = 17;
            btnShowHideInfo.Text = "Hide";
            // 
            // btnScheduleCancel
            // 
            btnScheduleCancel.BackColor = Color.White;
            btnScheduleCancel.BorderRadius = 12;
            btnScheduleCancel.CustomizableEdges = customizableEdges323;
            btnScheduleCancel.FillColor = Color.FromArgb(224, 224, 224);
            btnScheduleCancel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnScheduleCancel.ForeColor = Color.Gray;
            btnScheduleCancel.Location = new Point(5, 637);
            btnScheduleCancel.Name = "btnScheduleCancel";
            btnScheduleCancel.ShadowDecoration.CustomizableEdges = customizableEdges324;
            btnScheduleCancel.Size = new Size(84, 33);
            btnScheduleCancel.TabIndex = 29;
            btnScheduleCancel.Text = "Cancel";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label14.ForeColor = Color.Black;
            label14.Location = new Point(255, 289);
            label14.Name = "label14";
            label14.Size = new Size(65, 17);
            label14.TabIndex = 28;
            label14.Text = "Schift №2";
            // 
            // inputScheduleName
            // 
            inputScheduleName.BorderColor = Color.FromArgb(224, 224, 224);
            inputScheduleName.BorderRadius = 10;
            inputScheduleName.CustomizableEdges = customizableEdges325;
            inputScheduleName.DefaultText = "";
            inputScheduleName.Font = new Font("Segoe UI", 9F);
            inputScheduleName.ForeColor = Color.Black;
            inputScheduleName.Location = new Point(126, 69);
            inputScheduleName.Name = "inputScheduleName";
            inputScheduleName.PlaceholderText = "Write here...";
            inputScheduleName.SelectedText = "";
            inputScheduleName.ShadowDecoration.CustomizableEdges = customizableEdges326;
            inputScheduleName.Size = new Size(211, 36);
            inputScheduleName.TabIndex = 3;
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label31.ForeColor = Color.Black;
            label31.Location = new Point(126, 49);
            label31.Name = "label31";
            label31.Size = new Size(99, 17);
            label31.TabIndex = 7;
            label31.Text = "Schedule Name";
            // 
            // btnGenerate
            // 
            btnGenerate.BackColor = Color.White;
            btnGenerate.BorderRadius = 12;
            btnGenerate.CustomizableEdges = customizableEdges327;
            btnGenerate.FillColor = Color.FromArgb(51, 71, 255);
            btnGenerate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnGenerate.ForeColor = Color.White;
            btnGenerate.Location = new Point(284, 637);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.ShadowDecoration.CustomizableEdges = customizableEdges328;
            btnGenerate.Size = new Size(90, 33);
            btnGenerate.TabIndex = 26;
            btnGenerate.Text = "Generate";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label13.ForeColor = Color.Black;
            label13.Location = new Point(126, 289);
            label13.Name = "label13";
            label13.Size = new Size(65, 17);
            label13.TabIndex = 27;
            label13.Text = "Schift №1";
            // 
            // inputMaxFull
            // 
            inputMaxFull.BackColor = Color.Transparent;
            inputMaxFull.BorderRadius = 10;
            inputMaxFull.CustomizableEdges = customizableEdges329;
            inputMaxFull.Font = new Font("Segoe UI", 9F);
            inputMaxFull.Location = new Point(255, 149);
            inputMaxFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxFull.Name = "inputMaxFull";
            inputMaxFull.ShadowDecoration.CustomizableEdges = customizableEdges330;
            inputMaxFull.Size = new Size(83, 36);
            inputMaxFull.TabIndex = 21;
            inputMaxFull.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label12.ForeColor = Color.Black;
            label12.Location = new Point(254, 114);
            label12.Name = "label12";
            label12.Size = new Size(124, 51);
            label12.TabIndex = 26;
            label12.Text = "Maximum full schift \r\ndays in month\r\n\r\n";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label11.ForeColor = Color.Black;
            label11.Location = new Point(126, 198);
            label11.Name = "label11";
            label11.Size = new Size(90, 34);
            label11.TabIndex = 25;
            label11.Text = "Maximum full \r\nschift days \r\n";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label10.ForeColor = Color.Black;
            label10.Location = new Point(127, 114);
            label10.Name = "label10";
            label10.Size = new Size(99, 34);
            label10.TabIndex = 24;
            label10.Text = "Maximum schift\r\ndays \r\n";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.Black;
            label9.Location = new Point(5, 198);
            label9.Name = "label9";
            label9.Size = new Size(106, 34);
            label9.TabIndex = 23;
            label9.Text = "Maximum hours \r\nfor employee";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Black;
            label4.Location = new Point(255, 215);
            label4.Name = "label4";
            label4.Size = new Size(89, 17);
            label4.TabIndex = 22;
            label4.Text = "Schedule Year";
            // 
            // inputShift2
            // 
            inputShift2.BorderRadius = 10;
            inputShift2.CustomizableEdges = customizableEdges331;
            inputShift2.DefaultText = "15:00 - 21:00";
            inputShift2.Font = new Font("Segoe UI", 9F);
            inputShift2.ForeColor = Color.Black;
            inputShift2.Location = new Point(255, 307);
            inputShift2.Name = "inputShift2";
            inputShift2.PlaceholderText = "";
            inputShift2.SelectedText = "";
            inputShift2.ShadowDecoration.CustomizableEdges = customizableEdges332;
            inputShift2.Size = new Size(99, 36);
            inputShift2.TabIndex = 13;
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label28.ForeColor = Color.Black;
            label28.Location = new Point(5, 287);
            label28.Name = "label28";
            label28.Size = new Size(102, 17);
            label28.TabIndex = 10;
            label28.Text = "Schedule Month";
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label30.ForeColor = Color.Black;
            label30.Location = new Point(6, 129);
            label30.Name = "label30";
            label30.Size = new Size(100, 17);
            label30.TabIndex = 8;
            label30.Text = "People per shift";
            // 
            // inputShift1
            // 
            inputShift1.BorderRadius = 10;
            inputShift1.CustomizableEdges = customizableEdges333;
            inputShift1.DefaultText = "09:00 - 15:00";
            inputShift1.Font = new Font("Segoe UI", 9F);
            inputShift1.ForeColor = Color.Black;
            inputShift1.Location = new Point(126, 307);
            inputShift1.Name = "inputShift1";
            inputShift1.PlaceholderText = "";
            inputShift1.SelectedText = "";
            inputShift1.ShadowDecoration.CustomizableEdges = customizableEdges334;
            inputShift1.Size = new Size(99, 36);
            inputShift1.TabIndex = 11;
            // 
            // guna2Button6
            // 
            guna2Button6.BackColor = Color.White;
            guna2Button6.BorderRadius = 15;
            guna2Button6.CustomizableEdges = customizableEdges335;
            guna2Button6.DisabledState.BorderColor = Color.DarkGray;
            guna2Button6.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button6.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button6.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button6.FillColor = Color.White;
            guna2Button6.FocusedColor = Color.White;
            guna2Button6.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button6.ForeColor = Color.Black;
            guna2Button6.Image = (Image)resources.GetObject("guna2Button6.Image");
            guna2Button6.ImageSize = new Size(15, 15);
            guna2Button6.Location = new Point(5, 4);
            guna2Button6.Name = "guna2Button6";
            guna2Button6.PressedColor = Color.White;
            guna2Button6.ShadowDecoration.CustomizableEdges = customizableEdges336;
            guna2Button6.Size = new Size(119, 35);
            guna2Button6.TabIndex = 1;
            guna2Button6.Tag = "Information";
            guna2Button6.Text = "Information";
            // 
            // inputMaxConsecutiveFull
            // 
            inputMaxConsecutiveFull.BackColor = Color.Transparent;
            inputMaxConsecutiveFull.BorderRadius = 10;
            inputMaxConsecutiveFull.CustomizableEdges = customizableEdges337;
            inputMaxConsecutiveFull.Font = new Font("Segoe UI", 9F);
            inputMaxConsecutiveFull.Location = new Point(126, 235);
            inputMaxConsecutiveFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveFull.Name = "inputMaxConsecutiveFull";
            inputMaxConsecutiveFull.ShadowDecoration.CustomizableEdges = customizableEdges338;
            inputMaxConsecutiveFull.Size = new Size(83, 36);
            inputMaxConsecutiveFull.TabIndex = 19;
            inputMaxConsecutiveFull.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label32.ForeColor = Color.Black;
            label32.Location = new Point(5, 51);
            label32.Name = "label32";
            label32.Size = new Size(76, 17);
            label32.TabIndex = 0;
            label32.Text = "Schedule ID";
            // 
            // numberScheduleId
            // 
            numberScheduleId.BackColor = Color.Transparent;
            numberScheduleId.BorderRadius = 10;
            numberScheduleId.CustomizableEdges = customizableEdges339;
            numberScheduleId.Enabled = false;
            numberScheduleId.Font = new Font("Segoe UI", 9F);
            numberScheduleId.Location = new Point(6, 72);
            numberScheduleId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberScheduleId.Name = "numberScheduleId";
            numberScheduleId.ShadowDecoration.CustomizableEdges = customizableEdges340;
            numberScheduleId.Size = new Size(83, 33);
            numberScheduleId.TabIndex = 1;
            numberScheduleId.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // inputMaxConsecutiveDays
            // 
            inputMaxConsecutiveDays.BackColor = Color.Transparent;
            inputMaxConsecutiveDays.BorderRadius = 10;
            inputMaxConsecutiveDays.CustomizableEdges = customizableEdges341;
            inputMaxConsecutiveDays.Font = new Font("Segoe UI", 9F);
            inputMaxConsecutiveDays.Location = new Point(127, 149);
            inputMaxConsecutiveDays.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveDays.Name = "inputMaxConsecutiveDays";
            inputMaxConsecutiveDays.ShadowDecoration.CustomizableEdges = customizableEdges342;
            inputMaxConsecutiveDays.Size = new Size(83, 36);
            inputMaxConsecutiveDays.TabIndex = 17;
            inputMaxConsecutiveDays.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // inputPeoplePerShift
            // 
            inputPeoplePerShift.BackColor = Color.Transparent;
            inputPeoplePerShift.BorderRadius = 10;
            inputPeoplePerShift.CustomizableEdges = customizableEdges343;
            inputPeoplePerShift.Font = new Font("Segoe UI", 9F);
            inputPeoplePerShift.Location = new Point(7, 149);
            inputPeoplePerShift.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputPeoplePerShift.Name = "inputPeoplePerShift";
            inputPeoplePerShift.ShadowDecoration.CustomizableEdges = customizableEdges344;
            inputPeoplePerShift.Size = new Size(83, 34);
            inputPeoplePerShift.TabIndex = 9;
            inputPeoplePerShift.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // inputMonth
            // 
            inputMonth.BackColor = Color.Transparent;
            inputMonth.BorderRadius = 10;
            inputMonth.CustomizableEdges = customizableEdges345;
            inputMonth.Font = new Font("Segoe UI", 9F);
            inputMonth.Location = new Point(6, 307);
            inputMonth.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
            inputMonth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            inputMonth.Name = "inputMonth";
            inputMonth.ShadowDecoration.CustomizableEdges = customizableEdges346;
            inputMonth.Size = new Size(83, 36);
            inputMonth.TabIndex = 7;
            inputMonth.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            inputMonth.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // inputYear
            // 
            inputYear.BackColor = Color.Transparent;
            inputYear.BorderRadius = 10;
            inputYear.CustomizableEdges = customizableEdges347;
            inputYear.Font = new Font("Segoe UI", 9F);
            inputYear.Location = new Point(255, 235);
            inputYear.Maximum = new decimal(new int[] { 4000, 0, 0, 0 });
            inputYear.Minimum = new decimal(new int[] { 1900, 0, 0, 0 });
            inputYear.Name = "inputYear";
            inputYear.ShadowDecoration.CustomizableEdges = customizableEdges348;
            inputYear.Size = new Size(84, 36);
            inputYear.TabIndex = 5;
            inputYear.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            inputYear.Value = new decimal(new int[] { 2026, 0, 0, 0 });
            // 
            // inputMaxHours
            // 
            inputMaxHours.BackColor = Color.Transparent;
            inputMaxHours.BorderRadius = 10;
            inputMaxHours.CustomizableEdges = customizableEdges349;
            inputMaxHours.Font = new Font("Segoe UI", 9F);
            inputMaxHours.Location = new Point(5, 235);
            inputMaxHours.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxHours.Name = "inputMaxHours";
            inputMaxHours.ShadowDecoration.CustomizableEdges = customizableEdges350;
            inputMaxHours.Size = new Size(84, 36);
            inputMaxHours.TabIndex = 15;
            inputMaxHours.UpDownButtonFillColor = Color.FromArgb(224, 224, 224);
            // 
            // panel2
            // 
            panel2.Controls.Add(guna2GroupBox9);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(3, 3);
            panel2.Name = "panel2";
            panel2.Size = new Size(1137, 115);
            panel2.TabIndex = 34;
            // 
            // guna2GroupBox9
            // 
            guna2GroupBox9.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox9.BackColor = Color.Transparent;
            guna2GroupBox9.BorderColor = Color.White;
            guna2GroupBox9.BorderRadius = 20;
            guna2GroupBox9.BorderThickness = 0;
            guna2GroupBox9.Controls.Add(btnAddNewSchedule);
            guna2GroupBox9.Controls.Add(btnBackToScheduleList);
            guna2GroupBox9.Controls.Add(label33);
            guna2GroupBox9.Controls.Add(guna2Button11);
            guna2GroupBox9.Controls.Add(btnScheduleSave);
            guna2GroupBox9.Controls.Add(label34);
            guna2GroupBox9.CustomBorderColor = Color.White;
            guna2GroupBox9.CustomizableEdges = customizableEdges361;
            guna2GroupBox9.Font = new Font("Segoe UI", 9F);
            guna2GroupBox9.ForeColor = Color.White;
            guna2GroupBox9.Location = new Point(3, 4);
            guna2GroupBox9.Name = "guna2GroupBox9";
            guna2GroupBox9.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox9.ShadowDecoration.CustomizableEdges = customizableEdges362;
            guna2GroupBox9.ShadowDecoration.Depth = 7;
            guna2GroupBox9.ShadowDecoration.Enabled = true;
            guna2GroupBox9.ShadowDecoration.Shadow = new Padding(5, 0, 5, 5);
            guna2GroupBox9.Size = new Size(1129, 99);
            guna2GroupBox9.TabIndex = 30;
            // 
            // btnBackToScheduleList
            // 
            btnBackToScheduleList.Animated = true;
            btnBackToScheduleList.AutoRoundedCorners = true;
            btnBackToScheduleList.BorderColor = Color.White;
            btnBackToScheduleList.CustomizableEdges = customizableEdges355;
            btnBackToScheduleList.DisabledState.BorderColor = Color.DarkGray;
            btnBackToScheduleList.DisabledState.CustomBorderColor = Color.DarkGray;
            btnBackToScheduleList.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnBackToScheduleList.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnBackToScheduleList.FillColor = Color.FromArgb(231, 231, 231);
            btnBackToScheduleList.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBackToScheduleList.ForeColor = Color.DarkGray;
            btnBackToScheduleList.Image = (Image)resources.GetObject("btnBackToScheduleList.Image");
            btnBackToScheduleList.ImageSize = new Size(15, 15);
            btnBackToScheduleList.Location = new Point(6, 7);
            btnBackToScheduleList.Name = "btnBackToScheduleList";
            btnBackToScheduleList.ShadowDecoration.CustomizableEdges = customizableEdges356;
            btnBackToScheduleList.Size = new Size(72, 33);
            btnBackToScheduleList.TabIndex = 14;
            btnBackToScheduleList.Text = "Back";
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label33.ForeColor = Color.Black;
            label33.Location = new Point(6, 43);
            label33.Name = "label33";
            label33.Size = new Size(130, 28);
            label33.TabIndex = 15;
            label33.Text = "Schedule Edit";
            // 
            // guna2Button11
            // 
            guna2Button11.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2Button11.Animated = true;
            guna2Button11.BackColor = Color.Transparent;
            guna2Button11.BorderRadius = 12;
            guna2Button11.CustomizableEdges = customizableEdges357;
            guna2Button11.DisabledState.BorderColor = Color.DarkGray;
            guna2Button11.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button11.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button11.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button11.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button11.Font = new Font("Noto Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button11.ForeColor = Color.White;
            guna2Button11.ImageSize = new Size(15, 15);
            guna2Button11.Location = new Point(2515, 68);
            guna2Button11.Name = "guna2Button11";
            guna2Button11.ShadowDecoration.CustomizableEdges = customizableEdges358;
            guna2Button11.Size = new Size(105, 37);
            guna2Button11.TabIndex = 3;
            guna2Button11.Text = "Add New";
            // 
            // btnScheduleSave
            // 
            btnScheduleSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnScheduleSave.BackColor = Color.White;
            btnScheduleSave.BorderRadius = 12;
            btnScheduleSave.CustomizableEdges = customizableEdges359;
            btnScheduleSave.FillColor = Color.FromArgb(51, 71, 255);
            btnScheduleSave.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnScheduleSave.ForeColor = Color.White;
            btnScheduleSave.Location = new Point(993, 59);
            btnScheduleSave.Name = "btnScheduleSave";
            btnScheduleSave.ShadowDecoration.CustomizableEdges = customizableEdges360;
            btnScheduleSave.Size = new Size(129, 33);
            btnScheduleSave.TabIndex = 28;
            btnScheduleSave.Text = "Save Changes";
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label34.ForeColor = Color.Silver;
            label34.Location = new Point(6, 71);
            label34.Name = "label34";
            label34.Size = new Size(426, 42);
            label34.TabIndex = 16;
            label34.Text = "Here you can change or provide information about schedule\r\n\r\n";
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(button1);
            panel1.Controls.Add(guna2GroupBox15);
            panel1.Controls.Add(guna2GroupBox11);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1137, 1046);
            panel1.TabIndex = 33;
            // 
            // button1
            // 
            button1.Location = new Point(151, 1746);
            button1.Name = "button1";
            button1.Size = new Size(46, 28);
            button1.TabIndex = 34;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // guna2GroupBox15
            // 
            guna2GroupBox15.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox15.BackColor = Color.Transparent;
            guna2GroupBox15.BorderColor = Color.White;
            guna2GroupBox15.BorderRadius = 15;
            guna2GroupBox15.Controls.Add(guna2Button19);
            guna2GroupBox15.Controls.Add(guna2Button21);
            guna2GroupBox15.Controls.Add(guna2Button22);
            guna2GroupBox15.Controls.Add(guna2Button25);
            guna2GroupBox15.Controls.Add(dataGridAvailabilityOnScheduleEdit);
            guna2GroupBox15.CustomBorderColor = Color.White;
            guna2GroupBox15.CustomizableEdges = customizableEdges371;
            guna2GroupBox15.Font = new Font("Segoe UI", 9F);
            guna2GroupBox15.ForeColor = Color.Black;
            guna2GroupBox15.Location = new Point(432, 931);
            guna2GroupBox15.Name = "guna2GroupBox15";
            guna2GroupBox15.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox15.ShadowDecoration.CustomizableEdges = customizableEdges372;
            guna2GroupBox15.ShadowDecoration.Depth = 7;
            guna2GroupBox15.ShadowDecoration.Enabled = true;
            guna2GroupBox15.Size = new Size(675, 819);
            guna2GroupBox15.TabIndex = 33;
            // 
            // guna2Button19
            // 
            guna2Button19.BackColor = Color.White;
            guna2Button19.BorderRadius = 9;
            guna2Button19.CustomizableEdges = customizableEdges363;
            guna2Button19.DisabledState.BorderColor = Color.DarkGray;
            guna2Button19.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button19.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button19.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button19.FillColor = Color.White;
            guna2Button19.FocusedColor = Color.White;
            guna2Button19.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button19.ForeColor = Color.Black;
            guna2Button19.Image = (Image)resources.GetObject("guna2Button19.Image");
            guna2Button19.ImageSize = new Size(15, 15);
            guna2Button19.Location = new Point(5, 5);
            guna2Button19.Name = "guna2Button19";
            guna2Button19.PressedColor = Color.White;
            guna2Button19.ShadowDecoration.CustomizableEdges = customizableEdges364;
            guna2Button19.Size = new Size(176, 43);
            guna2Button19.TabIndex = 1;
            guna2Button19.Tag = "Information";
            guna2Button19.Text = "Availability Table";
            // 
            // guna2Button21
            // 
            guna2Button21.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button21.BorderRadius = 12;
            guna2Button21.CustomizableEdges = customizableEdges365;
            guna2Button21.DisabledState.BorderColor = Color.DarkGray;
            guna2Button21.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button21.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button21.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button21.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button21.Font = new Font("Segoe UI", 9F);
            guna2Button21.ForeColor = Color.White;
            guna2Button21.Location = new Point(977, 1835);
            guna2Button21.Name = "guna2Button21";
            guna2Button21.ShadowDecoration.CustomizableEdges = customizableEdges366;
            guna2Button21.Size = new Size(114, 33);
            guna2Button21.TabIndex = 7;
            guna2Button21.Text = "Save Changes";
            // 
            // guna2Button22
            // 
            guna2Button22.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            guna2Button22.Animated = true;
            guna2Button22.BorderRadius = 12;
            guna2Button22.CustomizableEdges = customizableEdges367;
            guna2Button22.DisabledState.BorderColor = Color.DarkGray;
            guna2Button22.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button22.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button22.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button22.FillColor = Color.FromArgb(224, 224, 224);
            guna2Button22.Font = new Font("Segoe UI", 9F);
            guna2Button22.ForeColor = Color.Gray;
            guna2Button22.Location = new Point(5, 1835);
            guna2Button22.Name = "guna2Button22";
            guna2Button22.ShadowDecoration.CustomizableEdges = customizableEdges368;
            guna2Button22.Size = new Size(84, 33);
            guna2Button22.TabIndex = 8;
            guna2Button22.Text = "Cancel";
            // 
            // guna2Button25
            // 
            guna2Button25.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button25.BorderRadius = 12;
            guna2Button25.CustomizableEdges = customizableEdges369;
            guna2Button25.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button25.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button25.ForeColor = Color.White;
            guna2Button25.Location = new Point(896, 1374);
            guna2Button25.Name = "guna2Button25";
            guna2Button25.ShadowDecoration.CustomizableEdges = customizableEdges370;
            guna2Button25.Size = new Size(129, 33);
            guna2Button25.TabIndex = 28;
            guna2Button25.Text = "Save Changes";
            // 
            // dataGridAvailabilityOnScheduleEdit
            // 
            dataGridViewCellStyle29.BackColor = Color.White;
            dataGridAvailabilityOnScheduleEdit.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle29;
            dataGridAvailabilityOnScheduleEdit.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridAvailabilityOnScheduleEdit.BackgroundColor = Color.Gainsboro;
            dataGridViewCellStyle30.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle30.BackColor = Color.FromArgb(100, 88, 255);
            dataGridViewCellStyle30.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle30.ForeColor = Color.White;
            dataGridViewCellStyle30.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle30.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle30.WrapMode = DataGridViewTriState.True;
            dataGridAvailabilityOnScheduleEdit.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle30;
            dataGridAvailabilityOnScheduleEdit.ColumnHeadersHeight = 32;
            dataGridViewCellStyle31.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle31.BackColor = Color.White;
            dataGridViewCellStyle31.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle31.ForeColor = Color.Black;
            dataGridViewCellStyle31.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dataGridViewCellStyle31.SelectionForeColor = Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle31.WrapMode = DataGridViewTriState.False;
            dataGridAvailabilityOnScheduleEdit.DefaultCellStyle = dataGridViewCellStyle31;
            dataGridAvailabilityOnScheduleEdit.GridColor = Color.FromArgb(231, 229, 255);
            dataGridAvailabilityOnScheduleEdit.Location = new Point(12, 54);
            dataGridAvailabilityOnScheduleEdit.Name = "dataGridAvailabilityOnScheduleEdit";
            dataGridViewCellStyle32.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle32.BackColor = SystemColors.Control;
            dataGridViewCellStyle32.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle32.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle32.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle32.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle32.WrapMode = DataGridViewTriState.True;
            dataGridAvailabilityOnScheduleEdit.RowHeadersDefaultCellStyle = dataGridViewCellStyle32;
            dataGridAvailabilityOnScheduleEdit.RowHeadersVisible = false;
            dataGridAvailabilityOnScheduleEdit.Size = new Size(648, 751);
            dataGridAvailabilityOnScheduleEdit.TabIndex = 27;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.AlternatingRowsStyle.Font = null;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.BackColor = Color.Gainsboro;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.GridColor = Color.FromArgb(231, 229, 255);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.HeaderStyle.Height = 32;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.ReadOnly = false;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.BackColor = Color.White;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.Height = 25;
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dataGridAvailabilityOnScheduleEdit.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // guna2GroupBox11
            // 
            guna2GroupBox11.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox11.BackColor = Color.Transparent;
            guna2GroupBox11.BorderColor = Color.White;
            guna2GroupBox11.BorderRadius = 15;
            guna2GroupBox11.Controls.Add(this.btnCloseScheduleTable);
            guna2GroupBox11.Controls.Add(btnHideShowScheduleTable);
            guna2GroupBox11.Controls.Add(guna2Button12);
            guna2GroupBox11.Controls.Add(guna2Button13);
            guna2GroupBox11.Controls.Add(guna2Button14);
            guna2GroupBox11.Controls.Add(slotGrid);
            guna2GroupBox11.CustomBorderColor = Color.White;
            guna2GroupBox11.CustomizableEdges = customizableEdges383;
            guna2GroupBox11.Font = new Font("Segoe UI", 9F);
            guna2GroupBox11.ForeColor = Color.Black;
            guna2GroupBox11.Location = new Point(432, 120);
            guna2GroupBox11.Name = "guna2GroupBox11";
            guna2GroupBox11.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox11.ShadowDecoration.CustomizableEdges = customizableEdges384;
            guna2GroupBox11.ShadowDecoration.Depth = 7;
            guna2GroupBox11.ShadowDecoration.Enabled = true;
            guna2GroupBox11.Size = new Size(675, 794);
            guna2GroupBox11.TabIndex = 32;
            // 
            // guna2Button12
            // 
            guna2Button12.BackColor = Color.White;
            guna2Button12.BorderRadius = 9;
            guna2Button12.CustomizableEdges = customizableEdges377;
            guna2Button12.DisabledState.BorderColor = Color.DarkGray;
            guna2Button12.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button12.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button12.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button12.FillColor = Color.White;
            guna2Button12.FocusedColor = Color.White;
            guna2Button12.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button12.ForeColor = Color.Black;
            guna2Button12.Image = (Image)resources.GetObject("guna2Button12.Image");
            guna2Button12.ImageSize = new Size(15, 15);
            guna2Button12.Location = new Point(5, 5);
            guna2Button12.Name = "guna2Button12";
            guna2Button12.PressedColor = Color.White;
            guna2Button12.ShadowDecoration.CustomizableEdges = customizableEdges378;
            guna2Button12.Size = new Size(176, 43);
            guna2Button12.TabIndex = 1;
            guna2Button12.Tag = "Information";
            guna2Button12.Text = "Schedule Table";
            // 
            // guna2Button13
            // 
            guna2Button13.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button13.BorderRadius = 12;
            guna2Button13.CustomizableEdges = customizableEdges379;
            guna2Button13.DisabledState.BorderColor = Color.DarkGray;
            guna2Button13.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button13.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button13.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button13.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button13.Font = new Font("Segoe UI", 9F);
            guna2Button13.ForeColor = Color.White;
            guna2Button13.Location = new Point(587, 1216);
            guna2Button13.Name = "guna2Button13";
            guna2Button13.ShadowDecoration.CustomizableEdges = customizableEdges380;
            guna2Button13.Size = new Size(114, 33);
            guna2Button13.TabIndex = 7;
            guna2Button13.Text = "Save Changes";
            // 
            // guna2Button14
            // 
            guna2Button14.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            guna2Button14.Animated = true;
            guna2Button14.BorderRadius = 12;
            guna2Button14.CustomizableEdges = customizableEdges381;
            guna2Button14.DisabledState.BorderColor = Color.DarkGray;
            guna2Button14.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button14.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button14.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button14.FillColor = Color.FromArgb(224, 224, 224);
            guna2Button14.Font = new Font("Segoe UI", 9F);
            guna2Button14.ForeColor = Color.Gray;
            guna2Button14.Location = new Point(5, 1216);
            guna2Button14.Name = "guna2Button14";
            guna2Button14.ShadowDecoration.CustomizableEdges = customizableEdges382;
            guna2Button14.Size = new Size(84, 33);
            guna2Button14.TabIndex = 8;
            guna2Button14.Text = "Cancel";
            // 
            // slotGrid
            // 
            dataGridViewCellStyle33.BackColor = Color.White;
            slotGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle33;
            slotGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            slotGrid.BackgroundColor = Color.Gainsboro;
            dataGridViewCellStyle34.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle34.BackColor = Color.FromArgb(100, 88, 255);
            dataGridViewCellStyle34.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle34.ForeColor = Color.White;
            dataGridViewCellStyle34.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle34.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle34.WrapMode = DataGridViewTriState.True;
            slotGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle34;
            slotGrid.ColumnHeadersHeight = 32;
            dataGridViewCellStyle35.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle35.BackColor = Color.White;
            dataGridViewCellStyle35.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle35.ForeColor = Color.Black;
            dataGridViewCellStyle35.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dataGridViewCellStyle35.SelectionForeColor = Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle35.WrapMode = DataGridViewTriState.False;
            slotGrid.DefaultCellStyle = dataGridViewCellStyle35;
            slotGrid.GridColor = Color.FromArgb(231, 229, 255);
            slotGrid.Location = new Point(12, 48);
            slotGrid.Name = "slotGrid";
            dataGridViewCellStyle36.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle36.BackColor = SystemColors.Control;
            dataGridViewCellStyle36.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle36.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle36.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle36.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle36.WrapMode = DataGridViewTriState.True;
            slotGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle36;
            slotGrid.RowHeadersVisible = false;
            slotGrid.Size = new Size(648, 732);
            slotGrid.TabIndex = 27;
            slotGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            slotGrid.ThemeStyle.AlternatingRowsStyle.Font = null;
            slotGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            slotGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            slotGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            slotGrid.ThemeStyle.BackColor = Color.Gainsboro;
            slotGrid.ThemeStyle.GridColor = Color.FromArgb(231, 229, 255);
            slotGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            slotGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            slotGrid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            slotGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            slotGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            slotGrid.ThemeStyle.HeaderStyle.Height = 32;
            slotGrid.ThemeStyle.ReadOnly = false;
            slotGrid.ThemeStyle.RowsStyle.BackColor = Color.White;
            slotGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            slotGrid.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            slotGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            slotGrid.ThemeStyle.RowsStyle.Height = 25;
            slotGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            slotGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // tabScheduleProfile
            // 
            tabScheduleProfile.Controls.Add(guna2GroupBox14);
            tabScheduleProfile.Controls.Add(guna2GroupBox12);
            tabScheduleProfile.Controls.Add(guna2GroupBox13);
            tabScheduleProfile.Location = new Point(4, 44);
            tabScheduleProfile.Name = "tabScheduleProfile";
            tabScheduleProfile.Padding = new Padding(3);
            tabScheduleProfile.Size = new Size(1143, 1052);
            tabScheduleProfile.TabIndex = 4;
            tabScheduleProfile.Text = "Schedule Profile";
            tabScheduleProfile.UseVisualStyleBackColor = true;
            // 
            // guna2GroupBox14
            // 
            guna2GroupBox14.BackColor = Color.Transparent;
            guna2GroupBox14.BorderColor = Color.White;
            guna2GroupBox14.BorderRadius = 17;
            guna2GroupBox14.BorderThickness = 0;
            guna2GroupBox14.Controls.Add(lblScheduleNote);
            guna2GroupBox14.Controls.Add(lblScheduleMonth);
            guna2GroupBox14.Controls.Add(label20);
            guna2GroupBox14.Controls.Add(labelScheduleNoteTitle);
            guna2GroupBox14.Controls.Add(lblScheduleYear);
            guna2GroupBox14.Controls.Add(btnScheduleDelete);
            guna2GroupBox14.Controls.Add(lblScheduleFromContainer);
            guna2GroupBox14.Controls.Add(btnScheduleEdit);
            guna2GroupBox14.Controls.Add(label18);
            guna2GroupBox14.Controls.Add(btnScheduleProfileCancel);
            guna2GroupBox14.Controls.Add(label19);
            guna2GroupBox14.Controls.Add(lblScheduleId);
            guna2GroupBox14.Controls.Add(lbl12);
            guna2GroupBox14.Controls.Add(label35);
            guna2GroupBox14.Controls.Add(guna2Button24);
            guna2GroupBox14.Controls.Add(labelName);
            guna2GroupBox14.CustomBorderColor = Color.White;
            guna2GroupBox14.CustomizableEdges = customizableEdges395;
            guna2GroupBox14.Font = new Font("Segoe UI", 9F);
            guna2GroupBox14.ForeColor = Color.White;
            guna2GroupBox14.Location = new Point(13, 125);
            guna2GroupBox14.Name = "guna2GroupBox14";
            guna2GroupBox14.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox14.ShadowDecoration.CustomizableEdges = customizableEdges396;
            guna2GroupBox14.ShadowDecoration.Depth = 7;
            guna2GroupBox14.ShadowDecoration.Enabled = true;
            guna2GroupBox14.Size = new Size(353, 442);
            guna2GroupBox14.TabIndex = 35;
            // 
            // lblScheduleNote
            // 
            lblScheduleNote.BorderRadius = 10;
            lblScheduleNote.CustomizableEdges = customizableEdges385;
            lblScheduleNote.DefaultText = "";
            lblScheduleNote.Font = new Font("Segoe UI", 9F);
            lblScheduleNote.ForeColor = Color.Black;
            lblScheduleNote.Location = new Point(15, 208);
            lblScheduleNote.Multiline = true;
            lblScheduleNote.Name = "lblScheduleNote";
            lblScheduleNote.PlaceholderText = "None...";
            lblScheduleNote.ScrollBars = ScrollBars.Vertical;
            lblScheduleNote.SelectedText = "";
            lblScheduleNote.ShadowDecoration.CustomizableEdges = customizableEdges386;
            lblScheduleNote.Size = new Size(323, 179);
            lblScheduleNote.TabIndex = 17;
            // 
            // lblScheduleMonth
            // 
            lblScheduleMonth.AutoSize = true;
            lblScheduleMonth.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScheduleMonth.ForeColor = Color.Black;
            lblScheduleMonth.Location = new Point(169, 141);
            lblScheduleMonth.Name = "lblScheduleMonth";
            lblScheduleMonth.Size = new Size(35, 30);
            lblScheduleMonth.TabIndex = 14;
            lblScheduleMonth.Text = "12";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.ForeColor = Color.Gray;
            label20.Location = new Point(166, 126);
            label20.Name = "label20";
            label20.Size = new Size(97, 15);
            label20.TabIndex = 13;
            label20.Text = "Schedule Month:";
            // 
            // labelScheduleNoteTitle
            // 
            labelScheduleNoteTitle.AutoSize = true;
            labelScheduleNoteTitle.ForeColor = Color.Gray;
            labelScheduleNoteTitle.Location = new Point(12, 190);
            labelScheduleNoteTitle.Name = "labelScheduleNoteTitle";
            labelScheduleNoteTitle.Size = new Size(87, 15);
            labelScheduleNoteTitle.TabIndex = 15;
            labelScheduleNoteTitle.Text = "Schedule Note:";
            // 
            // lblScheduleYear
            // 
            lblScheduleYear.AutoSize = true;
            lblScheduleYear.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScheduleYear.ForeColor = Color.Black;
            lblScheduleYear.Location = new Point(15, 141);
            lblScheduleYear.Name = "lblScheduleYear";
            lblScheduleYear.Size = new Size(57, 30);
            lblScheduleYear.TabIndex = 12;
            lblScheduleYear.Text = "2025";
            // 
            // btnScheduleDelete
            // 
            btnScheduleDelete.BorderRadius = 12;
            btnScheduleDelete.CustomizableEdges = customizableEdges387;
            btnScheduleDelete.FillColor = Color.FromArgb(255, 94, 98);
            btnScheduleDelete.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnScheduleDelete.ForeColor = Color.White;
            btnScheduleDelete.Location = new Point(196, 403);
            btnScheduleDelete.Name = "btnScheduleDelete";
            btnScheduleDelete.ShadowDecoration.CustomizableEdges = customizableEdges388;
            btnScheduleDelete.Size = new Size(72, 33);
            btnScheduleDelete.TabIndex = 10;
            btnScheduleDelete.Text = "Delete";
            // 
            // lblScheduleFromContainer
            // 
            lblScheduleFromContainer.AutoSize = true;
            lblScheduleFromContainer.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScheduleFromContainer.ForeColor = Color.Black;
            lblScheduleFromContainer.Location = new Point(166, 67);
            lblScheduleFromContainer.Name = "lblScheduleFromContainer";
            lblScheduleFromContainer.Size = new Size(101, 21);
            lblScheduleFromContainer.TabIndex = 11;
            lblScheduleFromContainer.Text = "Oleh Protsun";
            // 
            // btnScheduleEdit
            // 
            btnScheduleEdit.BorderRadius = 12;
            btnScheduleEdit.CustomizableEdges = customizableEdges389;
            btnScheduleEdit.FillColor = Color.FromArgb(51, 71, 255);
            btnScheduleEdit.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnScheduleEdit.ForeColor = Color.White;
            btnScheduleEdit.ImageAlign = HorizontalAlignment.Left;
            btnScheduleEdit.Location = new Point(273, 403);
            btnScheduleEdit.Name = "btnScheduleEdit";
            btnScheduleEdit.ShadowDecoration.CustomizableEdges = customizableEdges390;
            btnScheduleEdit.Size = new Size(73, 33);
            btnScheduleEdit.TabIndex = 9;
            btnScheduleEdit.Text = "Edit";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.ForeColor = Color.Gray;
            label18.Location = new Point(198, 21);
            label18.Name = "label18";
            label18.Size = new Size(72, 15);
            label18.TabIndex = 7;
            label18.Text = "Schedule ID:";
            // 
            // btnScheduleProfileCancel
            // 
            btnScheduleProfileCancel.BorderRadius = 12;
            btnScheduleProfileCancel.CustomizableEdges = customizableEdges391;
            btnScheduleProfileCancel.FillColor = Color.FromArgb(224, 224, 224);
            btnScheduleProfileCancel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnScheduleProfileCancel.ForeColor = Color.DarkGray;
            btnScheduleProfileCancel.Location = new Point(8, 403);
            btnScheduleProfileCancel.Name = "btnScheduleProfileCancel";
            btnScheduleProfileCancel.ShadowDecoration.CustomizableEdges = customizableEdges392;
            btnScheduleProfileCancel.Size = new Size(67, 33);
            btnScheduleProfileCancel.TabIndex = 2;
            btnScheduleProfileCancel.Text = "Cancel";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.ForeColor = Color.Gray;
            label19.Location = new Point(12, 126);
            label19.Name = "label19";
            label19.Size = new Size(83, 15);
            label19.TabIndex = 6;
            label19.Text = "Schedule Year:";
            // 
            // lblScheduleId
            // 
            lblScheduleId.AutoSize = true;
            lblScheduleId.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScheduleId.ForeColor = Color.Black;
            lblScheduleId.Location = new Point(275, 21);
            lblScheduleId.Name = "lblScheduleId";
            lblScheduleId.Size = new Size(13, 15);
            lblScheduleId.TabIndex = 10;
            lblScheduleId.Text = "0";
            // 
            // lbl12
            // 
            lbl12.AutoSize = true;
            lbl12.ForeColor = Color.Gray;
            lbl12.Location = new Point(169, 52);
            lbl12.Name = "lbl12";
            lbl12.Size = new Size(144, 15);
            lbl12.TabIndex = 5;
            lbl12.Text = "Schedule From Container:";
            // 
            // label35
            // 
            label35.AutoSize = true;
            label35.ForeColor = Color.Gray;
            label35.Location = new Point(12, 52);
            label35.Name = "label35";
            label35.Size = new Size(93, 15);
            label35.TabIndex = 4;
            label35.Text = "Schedule Name:";
            // 
            // guna2Button24
            // 
            guna2Button24.BackColor = Color.White;
            guna2Button24.BorderRadius = 9;
            guna2Button24.CustomizableEdges = customizableEdges393;
            guna2Button24.DisabledState.BorderColor = Color.DarkGray;
            guna2Button24.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button24.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button24.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button24.FillColor = Color.White;
            guna2Button24.FocusedColor = Color.White;
            guna2Button24.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button24.ForeColor = Color.Black;
            guna2Button24.Image = (Image)resources.GetObject("guna2Button24.Image");
            guna2Button24.ImageSize = new Size(15, 15);
            guna2Button24.Location = new Point(6, 6);
            guna2Button24.Name = "guna2Button24";
            guna2Button24.PressedColor = Color.White;
            guna2Button24.ShadowDecoration.CustomizableEdges = customizableEdges394;
            guna2Button24.Size = new Size(186, 43);
            guna2Button24.TabIndex = 3;
            guna2Button24.Tag = "Information";
            guna2Button24.Text = "Profile Information";
            // 
            // labelName
            // 
            labelName.AutoSize = true;
            labelName.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelName.ForeColor = Color.Black;
            labelName.Location = new Point(12, 67);
            labelName.Name = "labelName";
            labelName.Size = new Size(91, 25);
            labelName.TabIndex = 0;
            labelName.Text = "GrafikF35";
            // 
            // guna2GroupBox12
            // 
            guna2GroupBox12.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox12.BackColor = Color.Transparent;
            guna2GroupBox12.BorderColor = Color.White;
            guna2GroupBox12.BorderRadius = 15;
            guna2GroupBox12.Controls.Add(guna2Button15);
            guna2GroupBox12.Controls.Add(guna2Button16);
            guna2GroupBox12.Controls.Add(guna2Button17);
            guna2GroupBox12.Controls.Add(scheduleSlotProfileGrid);
            guna2GroupBox12.Controls.Add(guna2Button18);
            guna2GroupBox12.CustomBorderColor = Color.White;
            guna2GroupBox12.CustomizableEdges = customizableEdges405;
            guna2GroupBox12.Font = new Font("Segoe UI", 9F);
            guna2GroupBox12.ForeColor = Color.Black;
            guna2GroupBox12.Location = new Point(383, 125);
            guna2GroupBox12.Name = "guna2GroupBox12";
            guna2GroupBox12.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox12.ShadowDecoration.CustomizableEdges = customizableEdges406;
            guna2GroupBox12.ShadowDecoration.Depth = 7;
            guna2GroupBox12.ShadowDecoration.Enabled = true;
            guna2GroupBox12.Size = new Size(752, 919);
            guna2GroupBox12.TabIndex = 34;
            // 
            // guna2Button15
            // 
            guna2Button15.BackColor = Color.White;
            guna2Button15.BorderRadius = 9;
            guna2Button15.CustomizableEdges = customizableEdges397;
            guna2Button15.DisabledState.BorderColor = Color.DarkGray;
            guna2Button15.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button15.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button15.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button15.FillColor = Color.White;
            guna2Button15.FocusedColor = Color.White;
            guna2Button15.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2Button15.ForeColor = Color.Black;
            guna2Button15.Image = (Image)resources.GetObject("guna2Button15.Image");
            guna2Button15.ImageSize = new Size(15, 15);
            guna2Button15.Location = new Point(5, 5);
            guna2Button15.Name = "guna2Button15";
            guna2Button15.PressedColor = Color.White;
            guna2Button15.ShadowDecoration.CustomizableEdges = customizableEdges398;
            guna2Button15.Size = new Size(176, 43);
            guna2Button15.TabIndex = 1;
            guna2Button15.Tag = "Information";
            guna2Button15.Text = "Availability Table";
            // 
            // guna2Button16
            // 
            guna2Button16.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button16.BorderRadius = 12;
            guna2Button16.CustomizableEdges = customizableEdges399;
            guna2Button16.DisabledState.BorderColor = Color.DarkGray;
            guna2Button16.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button16.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button16.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button16.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button16.Font = new Font("Segoe UI", 9F);
            guna2Button16.ForeColor = Color.White;
            guna2Button16.Location = new Point(1125, 1884);
            guna2Button16.Name = "guna2Button16";
            guna2Button16.ShadowDecoration.CustomizableEdges = customizableEdges400;
            guna2Button16.Size = new Size(114, 33);
            guna2Button16.TabIndex = 7;
            guna2Button16.Text = "Save Changes";
            // 
            // guna2Button17
            // 
            guna2Button17.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            guna2Button17.Animated = true;
            guna2Button17.BorderRadius = 12;
            guna2Button17.CustomizableEdges = customizableEdges401;
            guna2Button17.DisabledState.BorderColor = Color.DarkGray;
            guna2Button17.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button17.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button17.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button17.FillColor = Color.FromArgb(224, 224, 224);
            guna2Button17.Font = new Font("Segoe UI", 9F);
            guna2Button17.ForeColor = Color.Gray;
            guna2Button17.Location = new Point(5, 1884);
            guna2Button17.Name = "guna2Button17";
            guna2Button17.ShadowDecoration.CustomizableEdges = customizableEdges402;
            guna2Button17.Size = new Size(84, 33);
            guna2Button17.TabIndex = 8;
            guna2Button17.Text = "Cancel";
            // 
            // scheduleSlotProfileGrid
            // 
            dataGridViewCellStyle37.BackColor = Color.White;
            scheduleSlotProfileGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle37;
            scheduleSlotProfileGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleSlotProfileGrid.BackgroundColor = Color.Gainsboro;
            dataGridViewCellStyle38.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle38.BackColor = Color.FromArgb(100, 88, 255);
            dataGridViewCellStyle38.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle38.ForeColor = Color.White;
            dataGridViewCellStyle38.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle38.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle38.WrapMode = DataGridViewTriState.True;
            scheduleSlotProfileGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle38;
            scheduleSlotProfileGrid.ColumnHeadersHeight = 32;
            dataGridViewCellStyle39.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle39.BackColor = Color.White;
            dataGridViewCellStyle39.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle39.ForeColor = Color.Black;
            dataGridViewCellStyle39.SelectionBackColor = Color.FromArgb(231, 229, 255);
            dataGridViewCellStyle39.SelectionForeColor = Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle39.WrapMode = DataGridViewTriState.False;
            scheduleSlotProfileGrid.DefaultCellStyle = dataGridViewCellStyle39;
            scheduleSlotProfileGrid.GridColor = Color.FromArgb(231, 229, 255);
            scheduleSlotProfileGrid.Location = new Point(18, 49);
            scheduleSlotProfileGrid.Name = "scheduleSlotProfileGrid";
            dataGridViewCellStyle40.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle40.BackColor = SystemColors.Control;
            dataGridViewCellStyle40.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle40.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle40.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle40.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle40.WrapMode = DataGridViewTriState.True;
            scheduleSlotProfileGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle40;
            scheduleSlotProfileGrid.RowHeadersVisible = false;
            scheduleSlotProfileGrid.Size = new Size(716, 856);
            scheduleSlotProfileGrid.TabIndex = 1;
            scheduleSlotProfileGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White;
            scheduleSlotProfileGrid.ThemeStyle.AlternatingRowsStyle.Font = null;
            scheduleSlotProfileGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty;
            scheduleSlotProfileGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty;
            scheduleSlotProfileGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty;
            scheduleSlotProfileGrid.ThemeStyle.BackColor = Color.Gainsboro;
            scheduleSlotProfileGrid.ThemeStyle.GridColor = Color.FromArgb(231, 229, 255);
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F);
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            scheduleSlotProfileGrid.ThemeStyle.HeaderStyle.Height = 32;
            scheduleSlotProfileGrid.ThemeStyle.ReadOnly = false;
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.BackColor = Color.White;
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(71, 69, 94);
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.Height = 25;
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);
            scheduleSlotProfileGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(71, 69, 94);
            // 
            // guna2Button18
            // 
            guna2Button18.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            guna2Button18.BorderRadius = 12;
            guna2Button18.CustomizableEdges = customizableEdges403;
            guna2Button18.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button18.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button18.ForeColor = Color.White;
            guna2Button18.Location = new Point(1044, 1423);
            guna2Button18.Name = "guna2Button18";
            guna2Button18.ShadowDecoration.CustomizableEdges = customizableEdges404;
            guna2Button18.Size = new Size(129, 33);
            guna2Button18.TabIndex = 28;
            guna2Button18.Text = "Save Changes";
            // 
            // guna2GroupBox13
            // 
            guna2GroupBox13.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guna2GroupBox13.BackColor = Color.Transparent;
            guna2GroupBox13.BorderColor = Color.White;
            guna2GroupBox13.BorderRadius = 20;
            guna2GroupBox13.BorderThickness = 0;
            guna2GroupBox13.Controls.Add(btnBackToContainerProfileFromSheduleProfile);
            guna2GroupBox13.Controls.Add(label16);
            guna2GroupBox13.Controls.Add(guna2Button20);
            guna2GroupBox13.Controls.Add(label17);
            guna2GroupBox13.CustomBorderColor = Color.White;
            guna2GroupBox13.CustomizableEdges = customizableEdges411;
            guna2GroupBox13.Font = new Font("Segoe UI", 9F);
            guna2GroupBox13.ForeColor = Color.White;
            guna2GroupBox13.Location = new Point(7, 6);
            guna2GroupBox13.Name = "guna2GroupBox13";
            guna2GroupBox13.ShadowDecoration.BorderRadius = 20;
            guna2GroupBox13.ShadowDecoration.CustomizableEdges = customizableEdges412;
            guna2GroupBox13.ShadowDecoration.Depth = 7;
            guna2GroupBox13.ShadowDecoration.Enabled = true;
            guna2GroupBox13.ShadowDecoration.Shadow = new Padding(5, 0, 5, 5);
            guna2GroupBox13.Size = new Size(1128, 99);
            guna2GroupBox13.TabIndex = 33;
            // 
            // btnBackToContainerProfileFromSheduleProfile
            // 
            btnBackToContainerProfileFromSheduleProfile.Animated = true;
            btnBackToContainerProfileFromSheduleProfile.AutoRoundedCorners = true;
            btnBackToContainerProfileFromSheduleProfile.BorderColor = Color.White;
            btnBackToContainerProfileFromSheduleProfile.CustomizableEdges = customizableEdges407;
            btnBackToContainerProfileFromSheduleProfile.DisabledState.BorderColor = Color.DarkGray;
            btnBackToContainerProfileFromSheduleProfile.DisabledState.CustomBorderColor = Color.DarkGray;
            btnBackToContainerProfileFromSheduleProfile.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnBackToContainerProfileFromSheduleProfile.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnBackToContainerProfileFromSheduleProfile.FillColor = Color.FromArgb(231, 231, 231);
            btnBackToContainerProfileFromSheduleProfile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBackToContainerProfileFromSheduleProfile.ForeColor = Color.DarkGray;
            btnBackToContainerProfileFromSheduleProfile.Image = (Image)resources.GetObject("btnBackToContainerProfileFromSheduleProfile.Image");
            btnBackToContainerProfileFromSheduleProfile.ImageSize = new Size(15, 15);
            btnBackToContainerProfileFromSheduleProfile.Location = new Point(6, 7);
            btnBackToContainerProfileFromSheduleProfile.Name = "btnBackToContainerProfileFromSheduleProfile";
            btnBackToContainerProfileFromSheduleProfile.ShadowDecoration.CustomizableEdges = customizableEdges408;
            btnBackToContainerProfileFromSheduleProfile.Size = new Size(72, 33);
            btnBackToContainerProfileFromSheduleProfile.TabIndex = 14;
            btnBackToContainerProfileFromSheduleProfile.Text = "Back";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label16.ForeColor = Color.Black;
            label16.Location = new Point(6, 43);
            label16.Name = "label16";
            label16.Size = new Size(152, 28);
            label16.TabIndex = 15;
            label16.Text = "Schedule Profile";
            // 
            // guna2Button20
            // 
            guna2Button20.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2Button20.Animated = true;
            guna2Button20.BackColor = Color.Transparent;
            guna2Button20.BorderRadius = 12;
            guna2Button20.CustomizableEdges = customizableEdges409;
            guna2Button20.DisabledState.BorderColor = Color.DarkGray;
            guna2Button20.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button20.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button20.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button20.FillColor = Color.FromArgb(51, 71, 255);
            guna2Button20.Font = new Font("Noto Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button20.ForeColor = Color.White;
            guna2Button20.ImageSize = new Size(15, 15);
            guna2Button20.Location = new Point(3342, 68);
            guna2Button20.Name = "guna2Button20";
            guna2Button20.ShadowDecoration.CustomizableEdges = customizableEdges410;
            guna2Button20.Size = new Size(105, 37);
            guna2Button20.TabIndex = 3;
            guna2Button20.Text = "Add New";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label17.ForeColor = Color.Silver;
            label17.Location = new Point(6, 71);
            label17.Name = "label17";
            label17.Size = new Size(313, 21);
            label17.TabIndex = 16;
            label17.Text = "Here you can see a detailed schedule profile\r\n";
            // 
            // errorProviderContainer
            // 
            errorProviderContainer.ContainerControl = this;
            // 
            // errorProviderSchedule
            // 
            errorProviderSchedule.ContainerControl = this;
            // 
            // guna2Elipse1
            // 
            guna2Elipse1.BorderRadius = 20;
            guna2Elipse1.TargetControl = containerGrid;
            // 
            // guna2Elipse2
            // 
            guna2Elipse2.BorderRadius = 20;
            guna2Elipse2.TargetControl = scheduleGrid;
            // 
            // guna2Elipse3
            // 
            guna2Elipse3.BorderRadius = 20;
            guna2Elipse3.TargetControl = slotGrid;
            // 
            // guna2Elipse4
            // 
            guna2Elipse4.BorderRadius = 20;
            guna2Elipse4.TargetControl = scheduleSlotProfileGrid;
            // 
            // guna2Elipse5
            // 
            guna2Elipse5.BorderRadius = 20;
            guna2Elipse5.TargetControl = dataGridAvailabilityOnScheduleEdit;
            // 
            // btnAddNewSchedule
            // 
            btnAddNewSchedule.Animated = true;
            btnAddNewSchedule.AutoRoundedCorners = true;
            btnAddNewSchedule.BorderColor = Color.White;
            btnAddNewSchedule.CustomizableEdges = customizableEdges353;
            btnAddNewSchedule.DisabledState.BorderColor = Color.DarkGray;
            btnAddNewSchedule.DisabledState.CustomBorderColor = Color.DarkGray;
            btnAddNewSchedule.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnAddNewSchedule.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnAddNewSchedule.FillColor = Color.FromArgb(231, 231, 231);
            btnAddNewSchedule.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAddNewSchedule.ForeColor = Color.DarkGray;
            btnAddNewSchedule.Image = (Image)resources.GetObject("btnAddNewSchedule.Image");
            btnAddNewSchedule.ImageSize = new Size(15, 15);
            btnAddNewSchedule.Location = new Point(892, 59);
            btnAddNewSchedule.Name = "btnAddNewSchedule";
            btnAddNewSchedule.ShadowDecoration.CustomizableEdges = customizableEdges354;
            btnAddNewSchedule.Size = new Size(95, 33);
            btnAddNewSchedule.TabIndex = 29;
            btnAddNewSchedule.Text = "Add New";
            // 
            // btnHideShowScheduleTable
            // 
            btnHideShowScheduleTable.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHideShowScheduleTable.Animated = true;
            btnHideShowScheduleTable.AutoRoundedCorners = true;
            btnHideShowScheduleTable.BorderColor = Color.White;
            btnHideShowScheduleTable.CustomizableEdges = customizableEdges375;
            btnHideShowScheduleTable.DisabledState.BorderColor = Color.DarkGray;
            btnHideShowScheduleTable.DisabledState.CustomBorderColor = Color.DarkGray;
            btnHideShowScheduleTable.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnHideShowScheduleTable.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnHideShowScheduleTable.FillColor = Color.FromArgb(231, 231, 231);
            btnHideShowScheduleTable.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnHideShowScheduleTable.ForeColor = Color.DarkGray;
            btnHideShowScheduleTable.Image = (Image)resources.GetObject("btnHideShowScheduleTable.Image");
            btnHideShowScheduleTable.ImageSize = new Size(15, 15);
            btnHideShowScheduleTable.Location = new Point(601, 7);
            btnHideShowScheduleTable.Name = "btnHideShowScheduleTable";
            btnHideShowScheduleTable.ShadowDecoration.CustomizableEdges = customizableEdges376;
            btnHideShowScheduleTable.Size = new Size(68, 33);
            btnHideShowScheduleTable.TabIndex = 41;
            btnHideShowScheduleTable.Text = "Hide";
            // 
            // btnCloseScheduleTable
            // 
            this.btnCloseScheduleTable.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCloseScheduleTable.Animated = true;
            this.btnCloseScheduleTable.AutoRoundedCorners = true;
            this.btnCloseScheduleTable.BorderColor = Color.White;
            this.btnCloseScheduleTable.CustomizableEdges = customizableEdges373;
            this.btnCloseScheduleTable.DisabledState.BorderColor = Color.DarkGray;
            this.btnCloseScheduleTable.DisabledState.CustomBorderColor = Color.DarkGray;
            this.btnCloseScheduleTable.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            this.btnCloseScheduleTable.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            this.btnCloseScheduleTable.FillColor = Color.FromArgb(231, 231, 231);
            this.btnCloseScheduleTable.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.btnCloseScheduleTable.ForeColor = Color.DarkGray;
            this.btnCloseScheduleTable.Image = (Image)resources.GetObject("btnCloseScheduleTable.Image");
            this.btnCloseScheduleTable.ImageSize = new Size(15, 15);
            this.btnCloseScheduleTable.Location = new Point(527, 7);
            this.btnCloseScheduleTable.Name = "btnCloseScheduleTable";
            this.btnCloseScheduleTable.ShadowDecoration.CustomizableEdges = customizableEdges374;
            this.btnCloseScheduleTable.Size = new Size(68, 33);
            this.btnCloseScheduleTable.TabIndex = 42;
            this.btnCloseScheduleTable.Text = "Close";
            // 
            // ContainerView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1151, 1100);
            Controls.Add(tabControl);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ContainerView";
            Text = "Container";
            tabControl.ResumeLayout(false);
            tabList.ResumeLayout(false);
            guna2GroupBox1.ResumeLayout(false);
            guna2GroupBox1.PerformLayout();
            ((ISupportInitialize)containerGrid).EndInit();
            tabEdit.ResumeLayout(false);
            guna2GroupBox3.ResumeLayout(false);
            guna2GroupBox3.PerformLayout();
            ((ISupportInitialize)numberContainerId).EndInit();
            guna2GroupBox2.ResumeLayout(false);
            guna2GroupBox2.PerformLayout();
            tabProfile.ResumeLayout(false);
            guna2GroupBox8.ResumeLayout(false);
            guna2GroupBox4.ResumeLayout(false);
            guna2GroupBox4.PerformLayout();
            ((ISupportInitialize)scheduleGrid).EndInit();
            guna2GroupBox6.ResumeLayout(false);
            guna2GroupBox6.PerformLayout();
            guna2GroupBox7.ResumeLayout(false);
            guna2GroupBox7.PerformLayout();
            tabScheduleEdit.ResumeLayout(false);
            panel3.ResumeLayout(false);
            guna2GroupBox19.ResumeLayout(false);
            guna2GroupBox19.PerformLayout();
            guna2GroupBox16.ResumeLayout(false);
            guna2GroupBox5.ResumeLayout(false);
            guna2GroupBox5.PerformLayout();
            guna2GroupBox18.ResumeLayout(false);
            guna2GroupBox18.PerformLayout();
            guna2GroupBox17.ResumeLayout(false);
            guna2GroupBox17.PerformLayout();
            ((ISupportInitialize)inputMaxFull).EndInit();
            ((ISupportInitialize)inputMaxConsecutiveFull).EndInit();
            ((ISupportInitialize)numberScheduleId).EndInit();
            ((ISupportInitialize)inputMaxConsecutiveDays).EndInit();
            ((ISupportInitialize)inputPeoplePerShift).EndInit();
            ((ISupportInitialize)inputMonth).EndInit();
            ((ISupportInitialize)inputYear).EndInit();
            ((ISupportInitialize)inputMaxHours).EndInit();
            panel2.ResumeLayout(false);
            guna2GroupBox9.ResumeLayout(false);
            guna2GroupBox9.PerformLayout();
            panel1.ResumeLayout(false);
            guna2GroupBox15.ResumeLayout(false);
            ((ISupportInitialize)dataGridAvailabilityOnScheduleEdit).EndInit();
            guna2GroupBox11.ResumeLayout(false);
            ((ISupportInitialize)slotGrid).EndInit();
            tabScheduleProfile.ResumeLayout(false);
            guna2GroupBox14.ResumeLayout(false);
            guna2GroupBox14.PerformLayout();
            guna2GroupBox12.ResumeLayout(false);
            ((ISupportInitialize)scheduleSlotProfileGrid).EndInit();
            guna2GroupBox13.ResumeLayout(false);
            guna2GroupBox13.PerformLayout();
            ((ISupportInitialize)errorProviderContainer).EndInit();
            ((ISupportInitialize)errorProviderSchedule).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2TabControl tabControl;
        private TabPage tabList;
        private TabPage tabEdit;
        private TabPage tabProfile;
        private TabPage tabScheduleEdit;
        private TabPage tabScheduleProfile;
        private Guna.UI2.WinForms.Guna2DataGridView containerGrid;
        private Guna.UI2.WinForms.Guna2Button btnDelete;
        private Guna.UI2.WinForms.Guna2Button btnEdit;
        private Guna.UI2.WinForms.Guna2Button btnAdd;
        private Guna.UI2.WinForms.Guna2Button btnSearch;
        private Guna.UI2.WinForms.Guna2TextBox inputSearch;
        private Guna.UI2.WinForms.Guna2Button btnCancel;
        private Guna.UI2.WinForms.Guna2Button btnSave;
        private Guna.UI2.WinForms.Guna2TextBox inputContainerNote;
        private Label label3;
        private Guna.UI2.WinForms.Guna2TextBox inputContainerName;
        private Guna.UI2.WinForms.Guna2NumericUpDown numberContainerId;
        private Label lblContainerName;
        private Guna.UI2.WinForms.Guna2DataGridView scheduleGrid;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleSearch;
        private Guna.UI2.WinForms.Guna2Button btnScheduleSearch;
        private Guna.UI2.WinForms.Guna2Button btnScheduleAdd;
        private Guna.UI2.WinForms.Guna2Button btnCancelProfile;
        private Guna.UI2.WinForms.Guna2NumericUpDown numberScheduleId;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleName;
        private Guna.UI2.WinForms.Guna2ComboBox comboScheduleShop;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleNote;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputYear;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMonth;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputPeoplePerShift;
        private Guna.UI2.WinForms.Guna2TextBox inputShift1;
        private Guna.UI2.WinForms.Guna2TextBox inputShift2;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxHours;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxConsecutiveDays;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxConsecutiveFull;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxFull;
        private Guna2ComboBox comboScheduleAvailability;
        private Guna.UI2.WinForms.Guna2Button btnGenerate;
        private Guna.UI2.WinForms.Guna2DataGridView slotGrid;
        private Guna.UI2.WinForms.Guna2Button btnScheduleSave;
        private Guna.UI2.WinForms.Guna2Button btnScheduleCancel;
        private Guna.UI2.WinForms.Guna2DataGridView scheduleSlotProfileGrid;
        private Guna.UI2.WinForms.Guna2Button btnScheduleProfileCancel;
        private ErrorProvider errorProviderContainer;
        private ErrorProvider errorProviderSchedule;
        private Guna2Button btnScheduleDelete;
        private Guna2Button btnScheduleEdit;
        private Guna2GroupBox guna2GroupBox1;
        private Label label6;
        private Label label7;
        private Guna2Elipse guna2Elipse1;
        private Guna2GroupBox guna2GroupBox3;
        private Label label24;
        private Guna2Button guna2Button3;
        private Label label25;
        private Guna2GroupBox guna2GroupBox2;
        private Label label26;
        private Guna2Button guna2Button4;
        private Label label27;
        private Guna2Button btnBackToContainerList;
        private Guna2GroupBox guna2GroupBox8;
        private Guna2Button btnCancelProfile2;
        private Guna2Button guna2Button2;
        private Guna2Button guna2Button5;
        private Guna2Button guna2Button7;
        private Guna2Button guna2Button1;
        private Guna2GroupBox guna2GroupBox6;
        private Label label2;
        private Label labelId;
        private Label label8;
        private Label label1;
        private Guna2Button guna2Button9;
        private Guna2GroupBox guna2GroupBox7;
        private Guna2Button btnBackToContainerListFromProfile;
        private Label label22;
        private Guna2Button guna2Button10;
        private Label label23;
        private Guna2TextBox lblContainerNote;
        private Label label5;
        private Guna2GroupBox guna2GroupBox4;
        private Guna2Button guna2Button8;
        private Guna2Elipse guna2Elipse2;
        private Guna2GroupBox guna2GroupBox5;
        private Label label4;
        private Label label28;
        private Label label30;
        private Label label31;
        private Label labelScheduleShop;
        private Guna2Button guna2Button6;
        private Label label32;
        private Guna2GroupBox guna2GroupBox9;
        private Guna2Button btnBackToScheduleList;
        private Label label33;
        private Guna2Button guna2Button11;
        private Label label34;
        private Label label9;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label label13;
        private Label label14;
        private Label labelScheduleNoteEdit;
        private Label label15;
        private Guna2GroupBox guna2GroupBox11;
        private Guna2Button guna2Button12;
        private Guna2Button guna2Button13;
        private Guna2Button guna2Button14;
        private Guna2Elipse guna2Elipse3;
        private Guna2GroupBox guna2GroupBox12;
        private Guna2Button guna2Button15;
        private Guna2Button guna2Button16;
        private Guna2Button guna2Button17;
        private Guna2Button guna2Button18;
        private Guna2DataGridView guna2DataGridView1;
        private Guna2GroupBox guna2GroupBox13;
        private Guna2Button btnBackToContainerProfileFromSheduleProfile;
        private Label label16;
        private Guna2Button guna2Button20;
        private Label label17;
        private Guna2GroupBox guna2GroupBox14;
        private Label label18;
        private Label label19;
        private Guna2Button guna2Button23;
        private Label lblScheduleId;
        private Label lbl12;
        private Label label35;
        private Guna2Button guna2Button24;
        private Label labelName;
        private Label lblScheduleFromContainer;
        private Label lblScheduleYear;
        private Label lblScheduleMonth;
        private Label label20;
        private Label labelScheduleNoteTitle;
        private Guna2Elipse guna2Elipse4;
        private Panel panel1;
        private Panel panel2;
        private Guna2GroupBox guna2GroupBox15;
        private Guna2Button guna2Button19;
        private Guna2Button guna2Button21;
        private Guna2Button guna2Button22;
        private Guna2Button guna2Button25;
        private Guna2DataGridView dataGridAvailabilityOnScheduleEdit;
        private Button button1;
        private Guna2Button btnShowHideInfo;
        private Guna2Elipse guna2Elipse5;
        private Guna2GroupBox guna2GroupBox16;
        private Guna2Button btnShowHideNote;
        private Guna2Button guna2Button29;
        private Guna2TextBox lblScheduleNote;
        private NoFocusScrollPanel panel3;
        private Guna2GroupBox guna2GroupBox17;
        private Label label21;
        private Label lbShopId;
        private Guna2Button btnSearchEmployeeInAvailabilityEdit;
        private Guna2Button btnSearchShopFromScheduleEdit;
        private Guna2TextBox textBoxSearchValueFromScheduleEdit;
        private Guna2TextBox textBoxSearchValue2FromScheduleEdit;
        private Guna2GroupBox guna2GroupBox18;
        private Guna2Button guna2Button26;
        private Guna2TextBox guna2TextBox1;
        private Guna2Button btnSearchAvailabilityFromScheduleEdit;
        private Guna2GroupBox guna2GroupBox19;
        private Guna2Button guna2Button28;
        private Label label37;
        private Guna2Button guna2Button30;
        private Guna2TextBox textBoxSearchValue3FromScheduleEdit;
        private Guna2Button btnRemoveEmployeeFromGroup;
        private Label lblEmployeeId;
        private Guna2ComboBox comboboxEmployee;
        private Label label29;
        private Guna2Button btnAddEmployeeToGroup;
        private Guna2Button btnShowHideEmployee;
        private Label label36;
        private Label lblAvailabilityID;
        private Label label39;
        private Guna2Button btnAddNewSchedule;
        private Guna2Button btnCloseScheduleTable;
        private Guna2Button guna2Button31;
        private Guna2Button btnHideShowScheduleTable;
    }
}
