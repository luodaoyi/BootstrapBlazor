﻿// Copyright (c) Argo Zhang (argo@163.com). All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Website: https://www.blazor.zone or https://argozhang.github.io/

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.Components;

public class ValidateFormTest : BootstrapBlazorTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddBootstrapBlazor();
        services.ConfigureJsonLocalizationOptions(op => op.AdditionalJsonAssemblies = new[] { GetType().Assembly });
    }

    [Fact]
    public void BootstrapBlazorDataAnnotationsValidator_Error()
    {
        Assert.ThrowsAny<InvalidOperationException>(() => Context.RenderComponent<BootstrapBlazorDataAnnotationsValidator>());
    }

    [Fact]
    public async Task Validate_Ok()
    {
        var valid = false;
        var invalid = false;
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.ShowLabelTooltip, true);
            pb.Add(a => a.Model, foo);
            pb.Add(a => a.OnValidSubmit, context =>
            {
                valid = true;
                return Task.CompletedTask;
            });
            pb.Add(a => a.OnInvalidSubmit, context =>
            {
                invalid = true;
                return Task.CompletedTask;
            });
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueChanged, EventCallback.Factory.Create<string>(this, v => foo.Name = v));
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        Assert.True(invalid);

        await cut.InvokeAsync(() =>
        {
            cut.Find("input").Change("Test");
            form.Submit();
        });
        Assert.True(valid);
    }

    [Fact]
    public void OnFieldValueChanged_Ok()
    {
        var changed = false;
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.Add(a => a.OnFieldValueChanged, (fieldName, v) =>
            {
                changed = true;
            });
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueChanged, EventCallback.Factory.Create<string>(this, v => foo.Name = v));
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
        });
        var form = cut.Find("form");
        cut.InvokeAsync(() => cut.Find("input").Change("Test"));
        cut.InvokeAsync(() => form.Submit());
        Assert.True(changed);
    }

    [Fact]
    public void ValidateAllProperties_Ok()
    {
        var foo = new Foo();
        var invalid = false;
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.Add(a => a.ValidateAllProperties, true);
            pb.Add(a => a.OnInvalidSubmit, context =>
            {
                invalid = true;
                return Task.CompletedTask;
            });
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueChanged, EventCallback.Factory.Create<string>(this, v => foo.Name = v));
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
        });
        var form = cut.Find("form");
        cut.InvokeAsync(() => form.Submit());
        Assert.True(invalid);
    }

    [Fact]
    public void ShowRequiredMark_Ok()
    {
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.Add(a => a.ShowRequiredMark, true);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueChanged, EventCallback.Factory.Create<string>(this, v => foo.Name = v));
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
        });
        cut.Contains("required=\"true\"");

        cut.SetParametersAndRender(pb =>
        {
            pb.Add(a => a.ShowRequiredMark, false);
        });
        cut.DoesNotContain("required=\"true\"");
    }

    [Fact]
    public void ShowLabel_Ok()
    {
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.Add(a => a.ShowLabel, true);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueChanged, EventCallback.Factory.Create<string>(this, v => foo.Name = v));
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
        });
        cut.Contains("label");

        cut.SetParametersAndRender(pb =>
        {
            pb.Add(a => a.ShowLabel, false);
        });
        cut.DoesNotContain("label");
    }

    [Fact]
    public async Task SetError_Ok()
    {
        var foo = new Foo();
        var dummy = new Dummy();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
            pb.AddChildContent<DateTimePicker<DateTime?>>(pb =>
            {
                pb.Add(a => a.Value, dummy.Value);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(dummy, "Value", typeof(DateTime?)));
            });
        });
        await cut.InvokeAsync(() => cut.Instance.SetError("Name", "Test_SetError"));
        await cut.InvokeAsync(() => cut.Instance.SetError("Test.Name", "Test_SetError"));
        await cut.InvokeAsync(() => cut.Instance.SetError<Foo>(f => f.Name, "Name_SetError"));

        // 利用反射提高代码覆盖率
        var method = typeof(ValidateForm).GetMethod("TryGetValidator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var ret = method.Invoke(cut.Instance, [typeof(Dummy), "Test", null]);
        Assert.False((bool?)ret);
    }

    [Fact]
    public async Task SetError_UnaryExpression()
    {
        var foo = new Foo();
        var dummy = new Dummy();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, dummy);
            pb.AddChildContent<DateTimePicker<DateTime?>>(pb =>
            {
                pb.Add(a => a.Value, foo.DateTime);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "DateTime", typeof(DateTime?)));
            });
            pb.AddChildContent<DateTimePicker<DateTime?>>(pb =>
            {
                pb.Add(a => a.Value, dummy.Value);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(dummy, "Value", typeof(DateTime?)));
            });
        });
        await cut.InvokeAsync(() => cut.Instance.SetError<Dummy>(f => f.Value, "Name_SetError"));

        // 利用反射提高代码覆盖率
        var fieldInfo = cut.Instance.GetType().GetField("_validatorCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var cache = (ConcurrentDictionary<(string FieldName, Type ModelType), (FieldIdentifier FieldIdentifier, IValidateComponent ValidateComponent)>)fieldInfo.GetValue(cut.Instance)!;
        cache.Remove(("Value", typeof(Dummy)), out _);
        await cut.InvokeAsync(() => cut.Instance.SetError<Dummy>(f => f.Value, "Name_SetError"));
    }

    [Fact]
    public void MetadataTypeAttribute_Ok()
    {
        var foo = new Dummy();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<DateTimePicker<DateTime?>>(pb =>
            {
                pb.Add(a => a.Value, foo.Value);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Value", typeof(DateTime?)));
            });
        });
        var form = cut.Find("form");
        cut.InvokeAsync(() => form.Submit());
    }

    [Fact]
    public void Validate_Class_Ok()
    {
        var dummy = new Dummy();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, dummy);
            pb.Add(a => a.ValidateAllProperties, true);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, dummy.Foo.Name);
            });
            pb.AddChildContent<BootstrapInput<Foo>>(pb =>
            {
                pb.Add(a => a.Value, dummy.Foo);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(dummy, nameof(dummy.Foo), typeof(Foo)));
            });
        });
        var form = cut.Find("form");
        cut.InvokeAsync(() => form.Submit());
    }

    [Fact]
    public async Task ValidateAll_Ok()
    {
        var invalid = false;
        var dummy = new Dummy();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, dummy);
            pb.Add(a => a.ValidateAllProperties, false);
            pb.AddChildContent<BootstrapInput<Foo>>(pb =>
            {
                pb.Add(a => a.Value, dummy.Foo);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(dummy, nameof(dummy.Foo), typeof(Foo)));
            });
            pb.Add(a => a.OnInvalidSubmit, context =>
            {
                invalid = true;
                return Task.CompletedTask;
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        Assert.False(invalid);

        cut.SetParametersAndRender(pb =>
        {
            pb.Add(a => a.ValidateAllProperties, true);
        });
        await cut.InvokeAsync(() => form.Submit());
        Assert.True(invalid);
    }

    [Fact]
    public async Task Validate_UploadFile_Ok()
    {
        var foo = new Dummy() { File = "text.txt" };
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<ButtonUpload<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.File);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "File", typeof(string)));
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
    }

    [Fact]
    public async Task Validate_Localizer_Ok()
    {
        var foo = new MockFoo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Name", typeof(string)));
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal("Name is Required", msg);
    }

    [Fact]
    public async Task Validate_Attribute_Ok()
    {
        var foo = new MockFoo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Rule);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Rule", typeof(string)));
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal("Rule is Required", msg);
    }

    [Fact]
    public async Task Validate_MemberName_Ok()
    {
        var foo = new MockFoo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Member);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Member", typeof(string)));
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal("Member is Required", msg);
    }

    [Fact]
    public void Validate_Address_Ok()
    {
        var foo = new MockFoo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Address);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Address", typeof(string)));
            });
        });
        var form = cut.Find("form");
        cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal("Address must fill", msg);
    }

    [Fact]
    public async Task ValidateFormButton_Valid()
    {
        var tcs = new TaskCompletionSource<bool>();
        var valid = false;
        var foo = new Foo() { Name = "Test" };
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(v => v.Model, foo);
            pb.Add(v => v.OnValidSubmit, context =>
            {
                valid = true;
                tcs.SetResult(true);
                return Task.CompletedTask;
            });
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
            pb.AddChildContent<Button>(pb =>
            {
                pb.Add(b => b.IsAsync, true);
                pb.Add(b => b.ButtonType, ButtonType.Submit);
            });
        });
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        await tcs.Task;
        Assert.True(valid);
    }

    [Fact]
    public async Task ValidateFormButton_Invalid()
    {
        var tcs = new TaskCompletionSource<bool>();
        var valid = true;
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(v => v.Model, foo);
            pb.Add(a => a.OnInvalidSubmit, context =>
            {
                valid = false;
                tcs.SetResult(true);
                return Task.CompletedTask;
            });
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
            pb.AddChildContent<Button>(pb =>
            {
                pb.Add(b => b.IsAsync, true);
                pb.Add(b => b.ButtonType, ButtonType.Submit);
            });
        });
        await cut.InvokeAsync(() => cut.Find("form").Submit());
        await tcs.Task;
        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateFromCode_Ok()
    {
        var foo = new Foo();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Address);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Address", typeof(string)));
            });
        });
        Assert.Contains("form-control valid", cut.Markup);

        var form = cut.Instance;
        await cut.InvokeAsync(() => form.Validate());
        Assert.Contains("form-control invalid is-invalid", cut.Markup);
    }

    [Fact]
    public async Task Validate_Service_Ok()
    {
        var foo = new HasService();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Tag);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Tag", typeof(string)));
                pb.Add(a => a.ValidateRules, [new FormItemValidator(new HasServiceAttribute())]);
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal(HasServiceAttribute.Success, msg);
    }

    [Fact]
    public async Task RequiredValidator_Ok()
    {
        var foo = new HasService();
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(a => a.Model, foo);
            pb.AddChildContent<MockInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Tag);
                pb.Add(a => a.ValueExpression, Utility.GenerateValueExpression(foo, "Tag", typeof(string)));
                pb.Add(a => a.ValidateRules, [new RequiredValidator()]);
            });
        });
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());
        var msg = cut.FindComponent<MockInput<string>>().Instance.GetErrorMessage();
        Assert.Equal(HasServiceAttribute.Success, msg);
    }

    [Fact]
    public void DisableAutoSubmitFormByEnter_Ok()
    {
        var options = Context.Services.GetRequiredService<IOptionsMonitor<BootstrapBlazorOptions>>();
        options.CurrentValue.DisableAutoSubmitFormByEnter = true;

        var foo = new Foo() { Name = "Test" };
        var cut = Context.RenderComponent<ValidateForm>(pb =>
        {
            pb.Add(v => v.Model, foo);
            pb.AddChildContent<BootstrapInput<string>>(pb =>
            {
                pb.Add(a => a.Value, foo.Name);
                pb.Add(a => a.ValueExpression, foo.GenerateValueExpression());
            });
            pb.AddChildContent<Button>(pb =>
            {
                pb.Add(b => b.IsAsync, true);
                pb.Add(b => b.ButtonType, ButtonType.Submit);
            });
        });

        Assert.True(cut.Instance.DisableAutoSubmitFormByEnter);

        cut.SetParametersAndRender(pb =>
        {
            pb.Add(a => a.DisableAutoSubmitFormByEnter, false);
        });
        Assert.False(cut.Instance.DisableAutoSubmitFormByEnter);
    }

    [Fact]
    public void ValidateFieldAsync_Ok()
    {
        var form = new ValidateForm();
        var method = typeof(ValidateForm).GetMethod("ValidateFieldAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var context = new ValidationContext(new Foo());
        var result = new List<ValidationResult>();
        method.Invoke(form, [context, result]);
    }

    private class HasServiceAttribute : ValidationAttribute
    {
        public const string Success = "Has Service";
        public const string Error = "No Service";

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var hasService = validationContext.GetService<IServiceProvider>();
            if (hasService is null)
                return new(Error);
            else
                return new(Success);
        }
    }

    private class HasService
    {
        [HasService]
        public string? Tag { get; set; }
    }

    [MetadataType(typeof(DummyMetadata))]
    private class Dummy
    {
        public DateTime? Value { get; set; }

        public Foo Foo { get; set; } = new Foo();

        [Required]
        public string? File { get; set; }
    }

    private class DummyMetadata
    {
        [Required]
        public DateTime? Value { get; set; }
    }

    private class MockFoo
    {
        [Required(ErrorMessage = "{0} is Required")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "{0} must fill")]
        [Display(Name = "Address")]
        public string? Address { get; set; } = "test";

        [Required()]
        public string? Rule { get; set; }

        [EmailAddress()]
        public string? Member { get; set; } = "test";
    }

    private class MockInput<TValue> : BootstrapInput<TValue>
    {
        public string? GetErrorMessage() => base.ErrorMessage;
    }
}
