﻿using Orchard.DisplayManagement.ModelBinding;
using Orchard.DisplayManagement.Shapes;
using Orchard.DisplayManagement.Views;
using System;
using System.Threading.Tasks;

namespace Orchard.DisplayManagement.Handlers
{
    public class DisplayDriver<TModel> : IDisplayDriver<TModel>  where TModel : class
    {
        /// <summary>
        /// Creates a new strongly typed shape.
        /// </summary>
        public ShapeResult Shape<T>() where T : Shape, new()
        {
            return new ShapeResult(typeof(T).Name, ctx => ctx.ShapeFactory.Create<T>());
        }

        /// <summary>
        /// Creates a new strongly typed shape and initializes it if it needs to be rendered.
        /// </summary>
        public ShapeResult Shape<T>(Func<T, Task> initialize) where T : Shape, new()
        {
            return new ShapeResult(
                typeof(T).Name, 
                ctx => ctx.ShapeFactory.Create<T>(),
                shape => initialize((T)shape)
                );
        }

        /// <summary>
        /// If the shape needs to be rendered, it is created automatically from its type name.
        /// </summary>
        public ShapeResult Shape(string shapeType)
        {
            return new ShapeResult(shapeType, ctx => ctx.ShapeFactory.Create(shapeType));
        }

        /// <summary>
        /// If the shape needs to be rendered, it is created automatically from its type name.
        /// </summary>
        public ShapeResult Shape(string shapeType, Func<dynamic, Task> initialize)
        {
            return new ShapeResult(
                shapeType, 
                ctx => ctx.ShapeFactory.Create(shapeType),
                initialize
                );
        }

        /// <summary>
        /// If the shape needs to be rendered, it is created automatically from its type name and initialized with a model.
        /// </summary>
        public ShapeResult Shape(string shapeType, object model)
        {
            return new ShapeResult(shapeType, ctx => ctx.ShapeFactory.Create(shapeType, Arguments.From(model)));
        }

        /// <summary>
        /// If the shape needs to be rendered, it is created by the delegate.
        /// </summary>
        public ShapeResult Shape(string shapeType, Func<IBuildShapeContext, dynamic> shapeBuilder)
        {
            return new ShapeResult(shapeType, shapeBuilder);
        }

        /// <summary>
        /// If the shape needs to be rendered, it is created by the delegate.
        /// </summary>
        public ShapeResult Shape(string shapeType, Func<IBuildShapeContext, dynamic> shapeBuilder, Func<dynamic, Task> initialize)
        {
            return new ShapeResult(shapeType, shapeBuilder, initialize);
        }

        public CombinedResult Combine(params IDisplayResult[] results)
        {
            return new CombinedResult(results);
        }

        Task<IDisplayResult> IDisplayDriver<TModel>.BuildDisplayAsync(TModel model, BuildDisplayContext context)
        {
            return DisplayAsync(model);
        }

        Task<IDisplayResult> IDisplayDriver<TModel>.BuildEditorAsync(TModel model, BuildEditorContext context)
        {
            return EditAsync(model);
        }

        Task<IDisplayResult> IDisplayDriver<TModel>.UpdateEditorAsync(TModel model, UpdateEditorContext context)
        {
            return UpdateAsync(model, context.Updater);
        }

        public virtual Task<IDisplayResult> DisplayAsync(TModel model)
        {
            return Task.FromResult(Display(model));
        }

        public virtual Task<IDisplayResult> EditAsync(TModel model)
        {
            return Task.FromResult(Edit(model));
        }

        public virtual Task<IDisplayResult> UpdateAsync(TModel model, IUpdateModel updater)
        {
            return Task.FromResult(Update(model, updater));
        }

        public virtual IDisplayResult Display(TModel model)
        {
            return null;
        }

        public virtual IDisplayResult Edit(TModel model)
        {
            return null;
        }

        public virtual IDisplayResult Update(TModel model, IUpdateModel updater)
        {
            return null;
        }

    }
}
