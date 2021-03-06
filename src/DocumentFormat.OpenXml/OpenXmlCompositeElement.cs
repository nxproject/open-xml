// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DocumentFormat.OpenXml.Framework;
using DocumentFormat.OpenXml.Validation.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace DocumentFormat.OpenXml
{
    /// <summary>
    /// Represents the base class for composite elements.
    /// </summary>
    public abstract class OpenXmlCompositeElement : OpenXmlElement
    {
        private OpenXmlElement _lastChild;

        /// <summary>
        /// Initializes a new instance of the OpenXmlCompositeElement class.
        /// </summary>
        protected OpenXmlCompositeElement()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the OpenXmlCompositeElement class using the supplied outer XML.
        /// </summary>
        /// <param name="outerXml">The outer XML of the element.</param>
        protected OpenXmlCompositeElement(string outerXml)
            : base(outerXml)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OpenXmlCompositeElement class using the supplied collection of elements.
        /// </summary>
        /// <param name="childrenElements">A collection of elements.</param>
        protected OpenXmlCompositeElement(IEnumerable childrenElements)
            : this()
        {
            if (childrenElements == null)
            {
                throw new ArgumentNullException(nameof(childrenElements));
            }

            foreach (OpenXmlElement child in childrenElements)
            {
                AppendChild(child);
            }
        }

        /// <summary>
        /// Initializes a new instance of the OpenXmlCompositeElement class using the supplied collection of OpenXmlElement elements.
        /// </summary>
        /// <param name="childrenElements">A collection of OpenXmlElement elements.</param>
        protected OpenXmlCompositeElement(IEnumerable<OpenXmlElement> childrenElements)
            : this()
        {
            if (childrenElements == null)
            {
                throw new ArgumentNullException(nameof(childrenElements));
            }

            foreach (OpenXmlElement child in childrenElements)
            {
                AppendChild(child);
            }
        }

        /// <summary>
        /// Initializes a new instance of the OpenXmlCompositeElement using the supplied array of OpenXmlElement elements.
        /// </summary>
        /// <param name="childrenElements">An array of OpenXmlElement elements.</param>
        protected OpenXmlCompositeElement(params OpenXmlElement[] childrenElements)
            : this()
        {
            if (childrenElements == null)
            {
                throw new ArgumentNullException(nameof(childrenElements));
            }

            foreach (OpenXmlElement child in childrenElements)
            {
                AppendChild(child);
            }
        }

        #region public properties

        /// <summary>
        /// Gets the first child of the current OpenXmlElement element.
        /// </summary>
        /// <remarks>
        /// Returns null (Nothing in Visual Basic) if there is no such OpenXmlElement element.
        /// </remarks>
        public override OpenXmlElement FirstChild
        {
            get
            {
                MakeSureParsed();

                OpenXmlElement lastChild = _lastChild;

                if (lastChild != null)
                {
                    return lastChild.Next;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the last child of the current OpenXmlElement element.
        /// Returns null (Nothing in Visual Basic) if there is no such OpenXmlElement element.
        /// </summary>
        public override OpenXmlElement LastChild
        {
            get
            {
                MakeSureParsed();

                return _lastChild;
            }
        }

        /// <inheritdoc/>
        public override bool HasChildren
        {
            get
            {
                return LastChild != null;
            }
        }

        /// <inheritdoc/>
        public override string InnerText
        {
            get
            {
                StringBuilder innerText = new StringBuilder();

                foreach (OpenXmlElement child in ChildElements)
                {
                    innerText.Append(child.InnerText);
                }

                return innerText.ToString();
            }
        }

        /// <inheritdoc/>
        public override string InnerXml
        {
            set
            {
                // first, clear all children
                RemoveAllChildren();

                if (!string.IsNullOrEmpty(value))
                {
                    // create an outer XML by wrapping the InnerXml with this element.
                    // because XmlReader can not be created on InnerXml ( InnerXml may have several root elements ).
                    using (StringWriter w = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        using (XmlWriter writer2 = new XmlDOMTextWriter(w))
                        {
                            writer2.WriteStartElement(Prefix, LocalName, NamespaceUri);
                            writer2.WriteRaw(value);
                            writer2.WriteEndElement();
                        }

                        // create a temp element to parse the xml.
                        OpenXmlElement newElement = CloneNode(false);
                        newElement.OuterXml = w.ToString();

                        OpenXmlElement child = newElement.FirstChild;
                        OpenXmlElement next = null;

                        // then move all children to this element.
                        while (child != null)
                        {
                            next = child.NextSibling();

                            child = newElement.RemoveChild(child);

                            AppendChild(child);

                            child = next;
                        }
                    }
                }
            }
        }

        #endregion

        #region change children

        /// <summary>
        /// Adds the specified element to the element if it is a known child. This adds the element in the correct location according to the schema.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement element to append.</param>
        /// <param name="throwOnError">A flag to indicate if the method should throw if the child could not be added.</param>
        /// <returns>Success if the element was added, otherwise <c>false</c>.</returns>
        public bool AddChild(OpenXmlElement newChild, bool throwOnError = true)
        {
            if (newChild is null)
            {
                if (throwOnError)
                {
                    throw new ArgumentNullException(nameof(newChild));
                }

                return false;
            }

            var wasAdded = SetElement(newChild);

            if (throwOnError && !wasAdded)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsNotChild);
            }

            return wasAdded;
        }

        /// <summary>
        /// Appends the specified element to the end of the current element's list of child nodes.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement element to append.</param>
        /// <returns>The OpenXmlElement element that was appended. </returns>
        /// <remarks>Returns null if <paramref name="newChild"/> equals <c>null</c>.</remarks>
        public override T AppendChild<T>(T newChild)
        {
            if (newChild == null)
            {
                return null;
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            ElementInsertingEvent(newChild);

            OpenXmlElement prevNode = LastChild;
            OpenXmlElement nextNode = newChild;

            if (prevNode == null)
            {
                nextNode.Next = nextNode;
                _lastChild = nextNode;
            }
            else
            {
                nextNode.Next = prevNode.Next;
                prevNode.Next = nextNode;
                _lastChild = nextNode;
            }

            newChild.Parent = this;

            ElementInsertedEvent(newChild);

            return newChild;
        }

        /// <summary>
        /// Inserts the specified element immediately after the specified reference element.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement element to insert.</param>
        /// <param name="referenceChild">The OpenXmlElement element after which <paramref name="newChild"/> should be added. Must be a child of this element.</param>
        /// <returns>The OpenXmlElement element that was inserted.</returns>
        /// <remarks>Returns <c>null</c> if <paramref name="newChild"/> is null. Inserted as first child if <paramref name="referenceChild"/> is <c>null</c>.</remarks>
        public override T InsertAfter<T>(T newChild, OpenXmlElement referenceChild)
        {
            if (newChild == null)
            {
                return null;
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            if (referenceChild == null)
            {
                return PrependChild(newChild);
            }

            if (referenceChild.Parent != this)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsNotChild);
            }

            ElementInsertingEvent(newChild);

            OpenXmlElement nextNode = newChild;
            OpenXmlElement prevNode = referenceChild;

            Debug.Assert(nextNode != null);
            Debug.Assert(prevNode != null);

            if (prevNode == _lastChild)
            {
                nextNode.Next = prevNode.Next;
                prevNode.Next = nextNode;
                _lastChild = nextNode;
            }
            else
            {
                OpenXmlElement next = prevNode.Next;
                nextNode.Next = next;
                prevNode.Next = nextNode;
            }

            newChild.Parent = this;

            ElementInsertedEvent(newChild);

            return newChild;
        }

        /// <summary>
        /// Inserts the specified element immediately before the specified reference element.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement to insert.</param>
        /// <param name="referenceChild">The OpenXmlElement element before which <paramref name="newChild"/> should be added. Must be a child of this element.</param>
        /// <returns>The OpenXmlElement that was inserted.</returns>
        /// <remarks>Returns <c>null</c> if <paramref name="newChild"/> is null. Inserted as first child if <paramref name="referenceChild"/> is <c>null</c>.</remarks>
        public override T InsertBefore<T>(T newChild, OpenXmlElement referenceChild)
        {
            if (newChild == null)
            {
                return null;
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            if (referenceChild == null)
            {
                return AppendChild(newChild);
            }

            if (referenceChild.Parent != this)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsNotChild);
            }

            ElementInsertingEvent(newChild);

            OpenXmlElement prevNode = newChild;
            OpenXmlElement nextNode = referenceChild;

            Debug.Assert(nextNode != null);
            Debug.Assert(prevNode != null);

            if (nextNode == FirstChild)
            {
                prevNode.Next = nextNode;
                _lastChild.Next = prevNode;
            }
            else
            {
                OpenXmlElement previousSibling = nextNode.PreviousSibling();
                prevNode.Next = nextNode;
                previousSibling.Next = prevNode;
            }

            newChild.Parent = this;

            ElementInsertedEvent(newChild);

            return newChild;
        }

        /// <summary>
        /// Inserts the specified element at the specified index of the current element's children.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement element to insert.</param>
        /// <param name="index">The zero-based index to insert the element to.</param>
        /// <returns>The OpenXmlElement element that was inserted.</returns>
        /// <remarks>Returns <c>null</c> if <paramref name="newChild"/> equals <c>null</c>.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or is greater than the count of children.</exception>
        public override T InsertAt<T>(T newChild, int index)
        {
            if (newChild == null)
            {
                return null;
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            if (index < 0 || index > ChildElements.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            else if (index == 0)
            {
                return PrependChild(newChild);
            }
            else if (index == ChildElements.Count)
            {
                return AppendChild(newChild);
            }
            else
            {
                OpenXmlElement refChild = ChildElements[index];
                return InsertBefore(newChild, refChild);
            }
        }

        /// <summary>
        /// Inserts the specified element at the beginning of the current element's list of child nodes.
        /// </summary>
        /// <param name="newChild">The OpenXmlElement element to add.</param>
        /// <returns>The OpenXmlElement that was added.</returns>
        /// <remarks>Returns <c>null</c> if <paramref name="newChild"/> equals <c>null</c>.</remarks>
        public override T PrependChild<T>(T newChild)
        {
            if (newChild == null)
            {
                return null;
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            return InsertBefore(newChild, FirstChild);
        }

        /// <summary>
        /// Removes the specified child element.
        /// </summary>
        /// <param name="child">The element to remove. Must be a child of this element.</param>
        /// <returns>The element that was removed. </returns>
        /// <remarks>Returns <c>null</c> if <paramref name="child"/> is <c>null</c>.</remarks>
        public override T RemoveChild<T>(T child)
        {
            if (child == null)
            {
                return null;
            }

            if (child.Parent != this)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsNotChild);
            }

            T removedElement = child;
            OpenXmlElement last = _lastChild;

            ElementRemovingEvent(removedElement);

            if (removedElement == FirstChild)
            {
                if (removedElement == _lastChild)
                {
                    _lastChild = null;
                }
                else
                {
                    OpenXmlElement nextNode = removedElement.Next;
                    last.Next = nextNode;
                }
            }
            else if (removedElement == _lastChild)
            {
                OpenXmlElement prevNode = removedElement.PreviousSibling();
                OpenXmlElement next = removedElement.Next;
                prevNode.Next = next;
                _lastChild = prevNode;
            }
            else
            {
                OpenXmlElement prevNode = removedElement.PreviousSibling();
                OpenXmlElement next = removedElement.Next;

                prevNode.Next = next;
            }

            removedElement.Next = null;
            removedElement.Parent = null;

            ElementRemovedEvent(removedElement);

            return removedElement;
        }

        /// <summary>
        /// Removes all of the current element's child elements.
        /// </summary>
        public override void RemoveAllChildren()
        {
            OpenXmlElement element = FirstChild;
            while (element != null)
            {
                OpenXmlElement next = element.NextSibling();

                RemoveChild(element);

                element = next;
            }

            Debug.Assert(_lastChild == null);
        }

        /// <summary>
        /// Replaces one of the current element's child elements with another OpenXmlElement element.
        /// </summary>
        /// <param name="newChild">The new OpenXmlElement to put in the child list.</param>
        /// <param name="oldChild">The OpenXmlElement to be replaced in the child list. Must be a child of the current element.</param>
        /// <returns>The OpenXmlElement that was replaced.</returns>
        /// <remarks>Returns <c>null</c> if <paramref name="newChild"/> equals <c>null</c>.</remarks>
        public override T ReplaceChild<T>(OpenXmlElement newChild, T oldChild)
        {
            if (oldChild == null)
            {
                return null;
            }

            if (newChild == null)
            {
                throw new ArgumentNullException(nameof(newChild));
            }

            if (oldChild.Parent != this)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsNotChild);
            }

            if (newChild.Parent != null)
            {
                throw new InvalidOperationException(ExceptionMessages.ElementIsPartOfTree);
            }

            OpenXmlElement refChild = oldChild.NextSibling();
            RemoveChild(oldChild);
            InsertBefore(newChild, refChild);
            return oldChild;
        }

        #endregion

        /// <summary>
        /// Saves all of the current node's children to the specified XmlWriter.
        /// </summary>
        /// <param name="w">The XmlWriter at which to save the child nodes. </param>
        internal override void WriteContentTo(XmlWriter w)
        {
            if (HasChildren)
            {
                foreach (OpenXmlElement childElement in ChildElements)
                {
                    childElement.WriteTo(w);
                }
            }
        }

        /// <summary>
        /// Fires the ElementInserting event.
        /// </summary>
        /// <param name="element">The OpenXmlElement element to insert.</param>
        private void ElementInsertingEvent(OpenXmlElement element)
        {
            if (OpenXmlElementContext != null)
            {
                OpenXmlElementContext.ElementInsertingEvent(element, this);
            }
        }

        /// <summary>
        /// Fires the ElementInserted event.
        /// </summary>
        /// <param name="element">The OpenXmlElement element to insert.</param>
        private void ElementInsertedEvent(OpenXmlElement element)
        {
            if (OpenXmlElementContext != null)
            {
                OpenXmlElementContext.ElementInsertedEvent(element, this);
            }
        }

        /// <summary>
        /// Fires the ElementRemoving event.
        /// </summary>
        /// <param name="element">The OpenXmlElement element to remove.</param>
        private void ElementRemovingEvent(OpenXmlElement element)
        {
            if (OpenXmlElementContext != null)
            {
                OpenXmlElementContext.ElementRemovingEvent(element, this);
            }
        }

        /// <summary>
        /// Fires the ElementRemoved event.
        /// </summary>
        /// <param name="element">The OpenXmlElement element to be removed.</param>
        private void ElementRemovedEvent(OpenXmlElement element)
        {
            if (OpenXmlElementContext != null)
            {
                OpenXmlElementContext.ElementRemovedEvent(element, this);
            }
        }

        /// <summary>
        /// Populates the XML into a strong typed DOM tree.
        /// </summary>
        /// <param name="xmlReader">The XmlReader to read the XML content.</param>
        /// <param name="loadMode">Specifies a load mode that is either lazy or full.</param>
        private protected override void Populate(XmlReader xmlReader, OpenXmlLoadMode loadMode)
        {
            LoadAttributes(xmlReader);

            if (!xmlReader.IsEmptyElement)
            {
                xmlReader.Read(); // read this element

                while (!xmlReader.EOF)
                {
                    // O15:#3024890, OpenXmlCompositeElement ignores the Whitespace NodeType.
                    if (xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        xmlReader.Skip();
                        continue;
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        Debug.Assert(string.Equals(xmlReader.LocalName, LocalName, StringComparison.Ordinal));
                        xmlReader.Skip(); // move to next node
                        break;
                    }

                    OpenXmlElement element = ElementFactory(xmlReader);

                    // set parent before Load( ) call. AlternateContentChoice need parent info on loading.
                    element.Parent = this;

                    bool isACB = element is AlternateContent;
                    if (isACB && element.OpenXmlElementContext != null)
                    {
                        element.OpenXmlElementContext.ACBlockLevel++;
                    }

                    bool mcContextPushed = false;
                    if (!(element is OpenXmlMiscNode))
                    {
                        // push MC context based on the context of the child element to be loaded
                        mcContextPushed = PushMcContext(xmlReader);
                    }

                    //Process the element according to the MC behavior
                    var action = ElementAction.Normal;
                    if (OpenXmlElementContext != null && OpenXmlElementContext.MCSettings.ProcessMode != DocumentFormat.OpenXml.Packaging.MarkupCompatibilityProcessMode.NoProcess)
                    {
                        action = OpenXmlElementContext.MCContext.GetElementAction(element, OpenXmlElementContext.MCSettings.TargetFileFormatVersions);
                    }

                    element.Load(xmlReader, loadMode);

                    if (mcContextPushed)
                    {
                        PopMcContext();
                    }

                    if (isACB && element.OpenXmlElementContext != null)
                    {
                        element.OpenXmlElementContext.ACBlockLevel--;
                    }

                    switch (action)
                    {
                        case ElementAction.Normal:
                            {
                                AddANode(element);
                                break;
                            }

                        case ElementAction.Ignore:
                            {
                                element.Parent = null;
                                continue;
                            }

                        case ElementAction.ProcessContent:
                            {
                                element.Parent = null;
                                while (element.ChildElements.Count > 0)
                                {
                                    var node = element.FirstChild;
                                    node.Remove();
                                    OpenXmlElement newnode = null;

                                    // If node is an UnknowElement, we should try to see whether the parent element can load the node as strong typed element
                                    if (node is OpenXmlUnknownElement)
                                    {
                                        newnode = ElementFactory(node.Prefix, node.LocalName, node.NamespaceUri);
                                        if (!(newnode is OpenXmlUnknownElement))
                                        {
                                            // The following method will load teh element in MCMode.Full
                                            // since the node is already MC-processed when loading as unknown type, full loading the outerXml is fine
                                            newnode.OuterXml = node.OuterXml;

                                            // unnecessary xmlns attribute will be added, remove it.
                                            RemoveUnnecessaryExtAttr(node, newnode);
                                        }
                                        else
                                        {
                                            newnode = null;
                                        }
                                    }

                                    if (newnode != null)
                                    {
                                        AddANode(newnode);
                                    }
                                    else
                                    {
                                        //append the original node
                                        AddANode(node);
                                    }
                                }

                                break;
                            }

                        case ElementAction.ACBlock:
                            {
                                var effectiveNode = OpenXmlElementContext.MCContext.GetContentFromACBlock(element as AlternateContent, OpenXmlElementContext.MCSettings.TargetFileFormatVersions);
                                if (effectiveNode == null)
                                {
                                    break;
                                }

                                element.Parent = null;
                                effectiveNode.Parent = null;
                                while (effectiveNode.FirstChild != null)
                                {
                                    var node = effectiveNode.FirstChild;
                                    node.Remove();
                                    AddANode(node);
                                    node.CheckMustUnderstandAttr();
                                }

                                break;
                            }
                    }
                }
            }
            else
            {
                xmlReader.Skip();
            }

            // Set raw outer xml to empty to indicate that it passed
            RawOuterXml = string.Empty;
        }

        private static void RemoveUnnecessaryExtAttr(OpenXmlElement node, OpenXmlElement newnode)
        {
            // Reconstruct the _nsMappings for the new node based on the original node
            node.MakeSureParsed();
            if (newnode.NamespaceDeclField != null && node.NamespaceDeclField != null)
            {
                newnode.NamespaceDeclField = new List<KeyValuePair<string, string>>(node.NamespaceDeclField);
            }
        }

        private protected TElement GetElement<TElement>()
            where TElement : OpenXmlElement => Metadata.Particle.Get<TElement>(this);

        private protected bool SetElement(OpenXmlElement value)
            => Metadata.Particle.Set(this, value);

        private void AddANode(OpenXmlElement node)
        {
            node.Parent = this;
            if (_lastChild == null)
            {
                node.Next = node;
                _lastChild = node;
            }
            else
            {
                node.Next = _lastChild.Next;
                _lastChild.Next = node;
                _lastChild = node;
            }
        }
    }
}
