/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OrderedCollection.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Poderosa.Util.Collections {
    //���MRU�Ɏg�p����R���N�V�����B
    //  * ����������X�g��ێ����A
    //  * �O��������������r�֐��ŗv�f�����������ǂ������݂āA
    //  * �O���Ɏ����Ă���B
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    public class OrderedCollection<T> : IEnumerable<T> {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        /// <exclude/>
        public delegate bool Equality(T t1, T t2);

        private Equality _equality;
        private List<T> _data; //[0]���擪

        public OrderedCollection(Equality equality) {
            _equality = equality;
            _data = new List<T>();
        }

        public void Update(T element) {
            for (int i = 0; i < _data.Count; i++) {
                if (_equality(_data[i], element)) {
                    _data.RemoveAt(i);
                    break; //�������̂��݂������炻����������Ĕ�����
                }
            }
            _data.Insert(0, element);
        }

        public void Add(T element) { //�ӂ��̒ǉ�
            _data.Add(element);
        }

        public void Clear() {
            _data.Clear();
        }

        public int Count {
            get {
                return _data.Count;
            }
        }
        public T this[int index] {
            get {
                return _data[index];
            }
        }

        //���̏�������߂Đ؂�
        public void LimitCount(int value) {
            if (_data.Count > value) {
                _data.RemoveRange(_data.Count - 1, _data.Count - value);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _data.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { //�{���ɁAIEnumerator<T>��IEnumerator����h�����Ă�̂��ăN�\�ȊO�̉��҂ł��Ȃ���
            return _data.GetEnumerator();
        }

    }
}
