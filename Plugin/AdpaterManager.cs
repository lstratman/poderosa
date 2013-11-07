/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: AdpaterManager.cs,v 1.2 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Poderosa.Util.Collections;

namespace Poderosa {
    //���ʂ̓A�v���ŗB��B�eAdapterFactory���Ǘ�����
    internal class AdapterManager : IAdapterManager {

        //�ϊ�����I�u�W�F�N�g��Type����Ή�����Factory�̃R���N�V�����ւ̃}�b�s���O
        //�o�����̂ǂ��瑤�Ȃ̂��͖���^��q�˂�A����
        private TypedHashtable<Type, List<IDualDirectionalAdapterFactory>> _classToFactoryList;

        public AdapterManager() {
            _classToFactoryList = new TypedHashtable<Type, List<IDualDirectionalAdapterFactory>>();
        }

        #region IAdapterManager
        public void RegisterFactory(IDualDirectionalAdapterFactory factory) {
            Debug.Assert(factory.SourceType.IsClass, "source type must be a class");
            RegisterFactory(factory.SourceType, factory);
            Debug.Assert(factory.AdapterType.IsClass, "adapter type must be a class");
            RegisterFactory(factory.AdapterType, factory);
        }
        private void RegisterFactory(Type type, IDualDirectionalAdapterFactory factory) {
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(type);
            if (l == null) {
                l = new List<IDualDirectionalAdapterFactory>();
                _classToFactoryList.Add(type, l);
            }
            l.Add(factory);
        }

        public void RemoveFactory(IDualDirectionalAdapterFactory factory) {
            RemoveFactory(factory.SourceType, factory);
            RemoveFactory(factory.AdapterType, factory);
        }
        private void RemoveFactory(Type type, IDualDirectionalAdapterFactory factory) {
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(type);
            if (l != null) {
                l.Remove(factory);
                if (l.Count == 0)
                    _classToFactoryList.Remove(type);
            }
        }

        public IAdaptable GetAdapter(IAdaptable obj, Type adapter) {
            //�V���[�g�J�b�g: ���ڌ^������ꍇ��AdapterFactory�̑��݂Ɋ֌W�Ȃ��ϊ��\
            if (adapter.IsInstanceOfType(obj))
                return obj;

            //�T���Ă݂�
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(obj.GetType());
            if (l == null)
                return null;

            foreach (IDualDirectionalAdapterFactory f in l) {
                IAdaptable r = ChallengeUsingAdapterFactory(f, obj, adapter);
                if (r != null)
                    return r;
            }

            return null;
        }

        private IAdaptable ChallengeUsingAdapterFactory(IDualDirectionalAdapterFactory factory, IAdaptable obj, Type adapter) {
            Type dest = factory.SourceType == obj.GetType() ? factory.AdapterType : factory.SourceType; //�ϊ���N���X

            if (adapter.IsAssignableFrom(dest)) { //����Ȃ�r���S�Ƃ����Ă����B���݂͂��̃P�[�X�����Ȃ��͂�
                IAdaptable t = factory.SourceType == obj.GetType() ? factory.GetAdapter(obj) : factory.GetSource(obj);
                Debug.Assert(adapter.IsInstanceOfType(t));
                return t;
            }

            //���G�ȃP�[�X�B
            //�ϊ���̃I�u�W�F�N�g��GetAdapter��ǂ�ł݂�i�ċA�Ăяo���΍��K�v�j�A
            //�Q�X�e�b�v�ȏ��Factory���g�p����A�Ȃǂ��K�v�B
            //TODO ���ݖ��T�|�[�g
            return null;
        }

        //���Generic��
        T IAdapterManager.GetAdapter<T>(IAdaptable obj) {
            return (T)GetAdapter(obj, typeof(T));
        }
        #endregion

        private List<IDualDirectionalAdapterFactory> FindFactoryList(Type adapter) {
            return _classToFactoryList[adapter];
        }

    }
}
