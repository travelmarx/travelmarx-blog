class TestClass(object):
    """Creates a test class sample instance.

    .. remarks::

        A test class.

    :param x: Defines x.
    :type x: str
    :param y: Defines y.
    :type y: int
    """

    def __init__(self, x=None, y=0):
        """Initializes TestClass.

        :param x: Defines x.
        :type x: str
        :param y: Defines y.
        :type y: int
        """

    def foo(self, arg1, arg2):
        """ Docstring of function foo.

        :param arg1: Describing the first parameter of foo().
        :type arg1: bool
        :param arg2: Describing the second parameter of foo().
        :type arg2: list

        .. remarks::
        
            The remarks block may be before the parameter
            definitions in .py file but it always appears after them.

        """
    pass
